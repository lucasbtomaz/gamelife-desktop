using gamelife_api.Persistence;
using System.Text.Json;

return await RunAsync(args);

static async Task<int> RunAsync(string[] arguments)
{
    try
    {
        if (arguments.Length != 3 || arguments[0] is not ("snapshot" or "verify"))
            throw new ArgumentException("Uso: snapshot <banco> <snapshot.json> | verify <banco> <snapshot.json>");

        var databasePath = Path.GetFullPath(arguments[1]);
        var snapshotPath = Path.GetFullPath(arguments[2]);
        if (arguments[0] == "snapshot")
        {
            var snapshot = await SqliteDatabaseSafety.CapturarAsync(databasePath);
            Directory.CreateDirectory(Path.GetDirectoryName(snapshotPath)!);
            await File.WriteAllTextAsync(snapshotPath, JsonSerializer.Serialize(snapshot));
            Console.WriteLine("Snapshot seguro do banco criado.");
            return 0;
        }

        var before = JsonSerializer.Deserialize<SqliteDatabaseSnapshot>(
            await File.ReadAllTextAsync(snapshotPath))
            ?? throw new InvalidOperationException("O snapshot inicial do banco é inválido.");
        var after = await SqliteDatabaseSafety.CapturarAsync(databasePath);
        SqliteDatabaseSafety.ValidarPreservacao(before, after);
        File.Delete(snapshotPath);
        Console.WriteLine("Integridade e contagens do banco foram preservadas.");
        return 0;
    }
    catch (Exception exception)
    {
        Console.Error.WriteLine(exception.Message);
        return 1;
    }
}
