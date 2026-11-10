using Microsoft.Data.Sqlite;
using System.Security.Cryptography;

namespace gamelife_api.Persistence;

public sealed record SqliteDatabaseSnapshot(
    string Integridade,
    long Tamanho,
    string HashSha256,
    IReadOnlyDictionary<string, long> Contagens);

public static class SqliteDatabaseSafety
{
    public static async Task<SqliteDatabaseSnapshot> CapturarAsync(
        string databasePath,
        CancellationToken cancellationToken = default)
    {
        var fullPath = Path.GetFullPath(databasePath);
        if (!File.Exists(fullPath))
            throw new FileNotFoundException("O banco local do GameLife não foi encontrado.", fullPath);

        var builder = new SqliteConnectionStringBuilder
        {
            DataSource = fullPath,
            Mode = SqliteOpenMode.ReadOnly,
            Cache = SqliteCacheMode.Private,
            Pooling = false,
            ForeignKeys = true
        };
        await using var connection = new SqliteConnection(builder.ConnectionString);
        await connection.OpenAsync(cancellationToken);

        var integrity = await ExecuteScalarAsync(connection, "PRAGMA integrity_check;", cancellationToken);
        if (!integrity.Equals("ok", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException($"O SQLite falhou na verificação de integridade: {integrity}");

        var tables = new List<string>();
        await using (var command = connection.CreateCommand())
        {
            command.CommandText = "SELECT name FROM sqlite_master WHERE type = 'table' AND name NOT LIKE 'sqlite_%' ORDER BY name";
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
                tables.Add(reader.GetString(0));
        }

        var counts = new SortedDictionary<string, long>(StringComparer.Ordinal);
        foreach (var table in tables)
        {
            var quotedTable = table.Replace("\"", "\"\"");
            var value = await ExecuteScalarAsync(connection, $"SELECT COUNT(*) FROM \"{quotedTable}\";", cancellationToken);
            counts[table] = long.Parse(value, System.Globalization.CultureInfo.InvariantCulture);
        }

        var info = new FileInfo(fullPath);
        var hash = Convert.ToHexString(SHA256.HashData(await File.ReadAllBytesAsync(fullPath, cancellationToken)));
        return new SqliteDatabaseSnapshot(integrity, info.Length, hash, counts);
    }

    public static void ValidarPreservacao(
        SqliteDatabaseSnapshot before,
        SqliteDatabaseSnapshot after)
    {
        if (!after.Integridade.Equals("ok", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("O banco final não está íntegro.");

        foreach (var (table, countBefore) in before.Contagens)
        {
            var countAfter = after.Contagens.GetValueOrDefault(table);
            if (countAfter < countBefore)
                throw new InvalidOperationException(
                    $"A tabela '{table}' perdeu registros: {countBefore} antes e {countAfter} depois.");
        }
    }

    private static async Task<string> ExecuteScalarAsync(
        SqliteConnection connection,
        string sql,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        return Convert.ToString(
            await command.ExecuteScalarAsync(cancellationToken),
            System.Globalization.CultureInfo.InvariantCulture) ?? string.Empty;
    }
}
