using gamelife_api.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.Sqlite;
using System.IO;

namespace GameLife.Desktop;

public static class LocalDatabase
{
    public static string CaminhoPadrao => ObterCaminhoPadrao(AppContext.BaseDirectory);

    public static string CaminhoLegado => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
        "AppData",
        "Local",
        "GameLife",
        "data",
        "gamelife.db");

    public static string ObterCaminhoPadrao(string baseDirectory)
    {
        ValidarDiretorioPublicado(baseDirectory);
        var applicationDirectory = new DirectoryInfo(Path.GetFullPath(baseDirectory));
        var projectRoot = applicationDirectory.Parent!.Parent!;

        return Path.Combine(projectRoot.FullName, "data", "gamelife.db");
    }

    public static void ValidarDiretorioPublicado(string baseDirectory)
    {
        var directory = new DirectoryInfo(Path.GetFullPath(baseDirectory));
        var isLegacyInstallation = directory.Name.Equals("App", StringComparison.OrdinalIgnoreCase) &&
                                   directory.Parent?.Name.Equals("GameLife", StringComparison.OrdinalIgnoreCase) == true &&
                                   directory.Parent.Parent?.Name.Equals("Local", StringComparison.OrdinalIgnoreCase) == true &&
                                   directory.Parent.Parent.Parent?.Name.Equals("AppData", StringComparison.OrdinalIgnoreCase) == true;
        var isOfficialPublication = directory.Name.Equals("GameLife", StringComparison.OrdinalIgnoreCase) &&
                                    directory.Parent?.Name.Equals("artifacts", StringComparison.OrdinalIgnoreCase) == true &&
                                    directory.Parent.Parent is not null;

        if (isLegacyInstallation)
        {
            throw new InvalidOperationException(
                "Esta instalação antiga do GameLife foi desativada. Use o atalho oficial da Área de Trabalho.");
        }

        if (!isOfficialPublication)
        {
            throw new InvalidOperationException(
                "O GameLife só pode ser executado de 'artifacts\\GameLife'. Use o atalho oficial da Área de Trabalho.");
        }
    }

    public static string ObterConnectionString(string databasePath) =>
        $"Data Source={Path.GetFullPath(databasePath)};Foreign Keys=True;Pooling=False";

    public static string? CriarBackupAntesDeAlterar(string databasePath, string backupDirectory)
    {
        var fullPath = Path.GetFullPath(databasePath);
        if (!File.Exists(fullPath)) return null;

        Directory.CreateDirectory(backupDirectory);
        var backupPath = Path.Combine(
            Path.GetFullPath(backupDirectory),
            $"gamelife-before-change-{DateTime.UtcNow:yyyyMMdd-HHmmssfff}-{Guid.NewGuid():N}.db");
        var sourceBuilder = new SqliteConnectionStringBuilder
        {
            DataSource = fullPath,
            Mode = SqliteOpenMode.ReadOnly,
            Pooling = false
        };
        var destinationBuilder = new SqliteConnectionStringBuilder
        {
            DataSource = backupPath,
            Mode = SqliteOpenMode.ReadWriteCreate,
            Pooling = false
        };
        using var source = new SqliteConnection(sourceBuilder.ConnectionString);
        using var destination = new SqliteConnection(destinationBuilder.ConnectionString);
        source.Open();
        destination.Open();
        source.BackupDatabase(destination);
        return backupPath;
    }

    public static async Task PrepararAsync(
        string databasePath,
        string? legacyMySqlConnectionString,
        CancellationToken cancellationToken = default,
        string? legacySqlitePath = null)
    {
        var fullPath = Path.GetFullPath(databasePath);
        var directory = Path.GetDirectoryName(fullPath)
            ?? throw new InvalidOperationException("O diretório do banco local não foi encontrado.");
        Directory.CreateDirectory(directory);
        var databaseAlreadyExisted = File.Exists(fullPath);

        var sqliteOptions = new DbContextOptionsBuilder<GameLifeDbContext>()
            .UseSqlite(ObterConnectionString(fullPath))
            .Options;

        try
        {
            await using var local = new GameLifeDbContext(sqliteOptions);
            await local.Database.EnsureCreatedAsync(cancellationToken);
            var legacyPath = legacySqlitePath;
            var legacyExists = !string.IsNullOrWhiteSpace(legacyPath) && File.Exists(legacyPath);
            var legacyNeedsMerge = legacyExists &&
                                   File.GetLastWriteTimeUtc(legacyPath!) > File.GetLastWriteTimeUtc(fullPath);
            var trackingTableExists = await TabelaExisteAsync(local, "AcompanhamentosJogos", cancellationToken);
            var uniqueOwnershipIndexExists = await IndiceExisteAsync(
                local, "IX_PossesJogos_JogoId_CanalId", cancellationToken);
            if (databaseAlreadyExisted &&
                (!trackingTableExists || !uniqueOwnershipIndexExists || legacyNeedsMerge))
            {
                var projectRoot = Directory.GetParent(directory)?.FullName ?? directory;
                CriarBackupAntesDeAlterar(fullPath, Path.Combine(projectRoot, "Backups"));
            }
            if (!uniqueOwnershipIndexExists)
                await ValidarPosseUnicaPorPlataformaAsync(local, cancellationToken);
            await AtualizarEsquemaAsync(local, cancellationToken);

            if (legacyNeedsMerge &&
                !Path.GetFullPath(legacyPath!).Equals(fullPath, StringComparison.OrdinalIgnoreCase))
            {
                var legacySqliteOptions = new DbContextOptionsBuilder<GameLifeDbContext>()
                    .UseSqlite(ObterConnectionString(legacyPath!))
                    .Options;
                await using var legacySqlite = new GameLifeDbContext(legacySqliteOptions);
                await LegacyDatabaseMigration.MesclarAsync(legacySqlite, local, cancellationToken);
            }

            if (!databaseAlreadyExisted &&
                !string.IsNullOrWhiteSpace(legacyMySqlConnectionString))
            {
                var legacyOptions = new DbContextOptionsBuilder<GameLifeDbContext>()
                    .UseMySQL(legacyMySqlConnectionString!)
                    .Options;
                await using var legacy = new GameLifeDbContext(legacyOptions);
                await LegacyDatabaseMigration.CopiarAsync(legacy, local, cancellationToken);
            }
        }
        catch
        {
            if (!databaseAlreadyExisted && File.Exists(fullPath))
                File.Delete(fullPath);
            throw;
        }
    }

    private static async Task<bool> TabelaExisteAsync(
        GameLifeDbContext contexto,
        string tableName,
        CancellationToken cancellationToken)
    {
        var connection = contexto.Database.GetDbConnection();
        await connection.OpenAsync(cancellationToken);
        try
        {
            await using var command = connection.CreateCommand();
            command.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' AND name = $name";
            var parameter = command.CreateParameter();
            parameter.ParameterName = "$name";
            parameter.Value = tableName;
            command.Parameters.Add(parameter);
            return Convert.ToInt64(await command.ExecuteScalarAsync(cancellationToken)) > 0;
        }
        finally
        {
            await connection.CloseAsync();
        }
    }

    private static async Task<bool> IndiceExisteAsync(
        GameLifeDbContext contexto,
        string indexName,
        CancellationToken cancellationToken)
    {
        var connection = contexto.Database.GetDbConnection();
        await connection.OpenAsync(cancellationToken);
        try
        {
            await using var command = connection.CreateCommand();
            command.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type = 'index' AND name = $name";
            var parameter = command.CreateParameter();
            parameter.ParameterName = "$name";
            parameter.Value = indexName;
            command.Parameters.Add(parameter);
            return Convert.ToInt64(await command.ExecuteScalarAsync(cancellationToken)) > 0;
        }
        finally
        {
            await connection.CloseAsync();
        }
    }

    private static async Task ValidarPosseUnicaPorPlataformaAsync(
        GameLifeDbContext contexto,
        CancellationToken cancellationToken)
    {
        var duplicate = await contexto.PossesJogos.AsNoTracking()
            .GroupBy(item => new { item.JogoId, item.CanalId })
            .Where(group => group.Count() > 1)
            .Select(group => new { group.Key.JogoId, group.Key.CanalId })
            .FirstOrDefaultAsync(cancellationToken);
        if (duplicate is not null)
        {
            throw new InvalidOperationException(
                $"O banco contém posses duplicadas para o jogo {duplicate.JogoId} e a plataforma {duplicate.CanalId}. " +
                "O backup foi criado e nenhum registro foi removido.");
        }
    }

    private static async Task AtualizarEsquemaAsync(
        GameLifeDbContext contexto,
        CancellationToken cancellationToken)
    {
        const string criarTabela = """
            CREATE TABLE IF NOT EXISTS "AcompanhamentosJogos" (
                "Id" INTEGER NOT NULL CONSTRAINT "PK_AcompanhamentosJogos" PRIMARY KEY AUTOINCREMENT,
                "JogoId" INTEGER NOT NULL,
                "CanalId" INTEGER NOT NULL,
                "PosseJogoId" INTEGER NULL,
                "Status" INTEGER NOT NULL,
                "DataStatus" TEXT NULL,
                "TempoEmHoras" TEXT NULL,
                "ConquistasPossiveis" INTEGER NULL,
                "ConquistasObtidas" INTEGER NULL,
                CONSTRAINT "FK_AcompanhamentosJogos_Jogos_JogoId" FOREIGN KEY ("JogoId") REFERENCES "Jogos" ("Id") ON DELETE RESTRICT,
                CONSTRAINT "FK_AcompanhamentosJogos_Canais_CanalId" FOREIGN KEY ("CanalId") REFERENCES "Canais" ("Id") ON DELETE RESTRICT,
                CONSTRAINT "FK_AcompanhamentosJogos_PossesJogos_PosseJogoId" FOREIGN KEY ("PosseJogoId") REFERENCES "PossesJogos" ("Id") ON DELETE RESTRICT
            );
            CREATE INDEX IF NOT EXISTS "IX_AcompanhamentosJogos_CanalId" ON "AcompanhamentosJogos" ("CanalId");
            CREATE UNIQUE INDEX IF NOT EXISTS "IX_AcompanhamentosJogos_JogoId_CanalId" ON "AcompanhamentosJogos" ("JogoId", "CanalId");
            CREATE INDEX IF NOT EXISTS "IX_AcompanhamentosJogos_PosseJogoId" ON "AcompanhamentosJogos" ("PosseJogoId");
            CREATE UNIQUE INDEX IF NOT EXISTS "IX_PossesJogos_JogoId_CanalId" ON "PossesJogos" ("JogoId", "CanalId");
            """;

        await contexto.Database.ExecuteSqlRawAsync(criarTabela, cancellationToken);
    }
}
