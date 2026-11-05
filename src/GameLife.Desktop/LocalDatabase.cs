using gamelife_api.Context;
using Microsoft.EntityFrameworkCore;
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
        var applicationDirectory = new DirectoryInfo(Path.GetFullPath(baseDirectory));
        var projectRoot = applicationDirectory.Name.Equals("GameLife", StringComparison.OrdinalIgnoreCase) &&
                          applicationDirectory.Parent?.Name.Equals("artifacts", StringComparison.OrdinalIgnoreCase) == true
            ? applicationDirectory.Parent.Parent
            : applicationDirectory;

        return Path.Combine(projectRoot!.FullName, "data", "gamelife.db");
    }

    public static string ObterConnectionString(string databasePath) =>
        $"Data Source={Path.GetFullPath(databasePath)};Foreign Keys=True;Pooling=False";

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
            await AtualizarEsquemaAsync(local, cancellationToken);

            if (!string.IsNullOrWhiteSpace(legacySqlitePath) &&
                File.Exists(legacySqlitePath) &&
                !Path.GetFullPath(legacySqlitePath).Equals(fullPath, StringComparison.OrdinalIgnoreCase))
            {
                var legacySqliteOptions = new DbContextOptionsBuilder<GameLifeDbContext>()
                    .UseSqlite(ObterConnectionString(legacySqlitePath))
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
            """;

        await contexto.Database.ExecuteSqlRawAsync(criarTabela, cancellationToken);
    }
}
