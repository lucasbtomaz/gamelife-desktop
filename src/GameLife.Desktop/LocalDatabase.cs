using gamelife_api.Context;
using Microsoft.EntityFrameworkCore;
using System.IO;

namespace GameLife.Desktop;

public static class LocalDatabase
{
    public static string CaminhoPadrao => ObterCaminhoPadrao(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));

    public static string ObterCaminhoPadrao(string userProfile) => Path.Combine(
        userProfile,
        "AppData",
        "Local",
        "GameLife",
        "data",
        "gamelife.db");

    public static string ObterConnectionString(string databasePath) =>
        $"Data Source={Path.GetFullPath(databasePath)};Foreign Keys=True;Pooling=False";

    public static async Task PrepararAsync(
        string databasePath,
        string? legacyMySqlConnectionString,
        CancellationToken cancellationToken = default)
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
}
