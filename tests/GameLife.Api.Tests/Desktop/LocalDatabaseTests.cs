using gamelife_api.Context;
using gamelife_api.Models;
using GameLife.Desktop;
using Microsoft.EntityFrameworkCore;

namespace gamelife_api.Tests.Desktop;

public sealed class LocalDatabaseTests : IDisposable
{
    private readonly string _directory = Path.Combine(
        Path.GetTempPath(),
        $"GameLifeLocalDatabase-{Guid.NewGuid():N}");

    [Fact]
    public void ResolveBancoForaDaPastaDoExecutavelPublicado()
    {
        var projectRoot = Path.Combine(_directory, "Projeto");
        var path = LocalDatabase.ObterCaminhoPadrao(
            Path.Combine(projectRoot, "artifacts", "GameLife"));

        Assert.Equal(Path.Combine(projectRoot, "data", "gamelife.db"), path);
        Assert.DoesNotContain(Path.Combine("artifacts", "GameLife", "data"), path);
    }

    [Fact]
    public void RejeitaExecutavelLegadoAntesDeCriarOutroBanco()
    {
        var legacyDirectory = Path.Combine(
            _directory, "AppData", "Local", "GameLife", "App");

        var exception = Assert.Throws<InvalidOperationException>(() =>
            LocalDatabase.ValidarDiretorioPublicado(legacyDirectory));

        Assert.Contains("atalho oficial", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.False(Directory.Exists(Path.Combine(legacyDirectory, "data")));
    }

    [Fact]
    public void RejeitaExecutavelForaDaPublicacaoOficialAntesDeCriarOutroBanco()
    {
        var alternativeDirectory = Path.Combine(_directory, "copia-alternativa");

        var exception = Assert.Throws<InvalidOperationException>(() =>
            LocalDatabase.ObterCaminhoPadrao(alternativeDirectory));

        Assert.Contains("artifacts", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("atalho oficial", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.False(Directory.Exists(Path.Combine(alternativeDirectory, "data")));
    }

    [Fact]
    public async Task PreparaBancoSqliteSemMySql()
    {
        var path = Path.Combine(_directory, "data", "gamelife.db");

        await LocalDatabase.PrepararAsync(path, legacyMySqlConnectionString: null);

        Assert.True(File.Exists(path));
        await using var context = NewContext(path);
        context.Jogos.Add(new Jogo { Titulo = "Jogo local" });
        await context.SaveChangesAsync();
        Assert.Equal(1, await context.Jogos.CountAsync());
    }

    [Fact]
    public async Task AtualizaBancoExistenteSemApagarDados()
    {
        var path = Path.Combine(_directory, "data", "existente.db");
        await using (var antigo = NewContext(path))
        {
            await antigo.Database.EnsureCreatedAsync();
            antigo.Jogos.Add(new Jogo { Titulo = "Jogo preservado" });
            await antigo.SaveChangesAsync();
            await antigo.Database.ExecuteSqlRawAsync("DROP TABLE AcompanhamentosJogos");
        }

        await LocalDatabase.PrepararAsync(path, legacyMySqlConnectionString: null);

        await using var atualizado = NewContext(path);
        Assert.Equal(1, await atualizado.Jogos.CountAsync());
        Assert.Equal(0, await atualizado.AcompanhamentosJogos.CountAsync());
    }

    [Fact]
    public async Task CriaIndiceUnicoDePosseSemAlterarDadosExistentes()
    {
        var path = Path.Combine(_directory, "data", "sem-indice-posse.db");
        await using (var antigo = NewContext(path))
        {
            await antigo.Database.EnsureCreatedAsync();
            await antigo.Database.ExecuteSqlRawAsync(
                "DROP INDEX IF EXISTS IX_PossesJogos_JogoId_CanalId");
            var jogo = new Jogo { Titulo = "Jogo preservado" };
            var canal = new Canal { Nome = "STEAM", PermitePosse = true };
            antigo.PossesJogos.Add(new PosseJogo { Jogo = jogo, Canal = canal, MidiaFisica = true });
            await antigo.SaveChangesAsync();
        }

        await LocalDatabase.PrepararAsync(path, legacyMySqlConnectionString: null);

        await using var atualizado = NewContext(path);
        Assert.Equal(1, await atualizado.Jogos.CountAsync());
        Assert.Equal(1, await atualizado.PossesJogos.CountAsync());
        var posse = await atualizado.PossesJogos.AsNoTracking().SingleAsync();
        atualizado.PossesJogos.Add(new PosseJogo
        {
            JogoId = posse.JogoId,
            CanalId = posse.CanalId,
            MidiaFisica = false
        });
        await Assert.ThrowsAsync<DbUpdateException>(() => atualizado.SaveChangesAsync());
    }

    [Fact]
    public async Task FazBackupERejeitaPossesDuplicadasSemApagarRegistros()
    {
        var path = Path.Combine(_directory, "data", "posses-duplicadas.db");
        await using (var antigo = NewContext(path))
        {
            await antigo.Database.EnsureCreatedAsync();
            await antigo.Database.ExecuteSqlRawAsync(
                "DROP INDEX IF EXISTS IX_PossesJogos_JogoId_CanalId");
            var jogo = new Jogo { Titulo = "Jogo duplicado" };
            var canal = new Canal { Nome = "PC", PermitePosse = true };
            antigo.PossesJogos.AddRange(
                new PosseJogo { Jogo = jogo, Canal = canal, MidiaFisica = false },
                new PosseJogo { Jogo = jogo, Canal = canal, MidiaFisica = true });
            await antigo.SaveChangesAsync();
        }

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            LocalDatabase.PrepararAsync(path, legacyMySqlConnectionString: null));

        Assert.Contains("duplicad", exception.Message, StringComparison.OrdinalIgnoreCase);
        await using var preservado = NewContext(path);
        Assert.Equal(2, await preservado.PossesJogos.CountAsync());
        var backups = Directory.GetFiles(Path.Combine(_directory, "Backups"), "*.db");
        var backup = Assert.Single(backups);
        await using var copia = NewContext(backup);
        Assert.Equal(2, await copia.PossesJogos.CountAsync());
    }

    [Fact]
    public async Task CriaBackupConsistenteSemAlterarBancoOriginal()
    {
        var databasePath = Path.Combine(_directory, "data", "gamelife.db");
        var backupDirectory = Path.Combine(_directory, "Backups");
        await using (var context = NewContext(databasePath))
        {
            await context.Database.EnsureCreatedAsync();
            context.Jogos.Add(new Jogo { Titulo = "Preservado" });
            await context.SaveChangesAsync();
        }
        var originalHash = Convert.ToHexString(
            System.Security.Cryptography.SHA256.HashData(File.ReadAllBytes(databasePath)));

        var backupPath = LocalDatabase.CriarBackupAntesDeAlterar(databasePath, backupDirectory);

        Assert.NotNull(backupPath);
        Assert.Equal(originalHash, Convert.ToHexString(
            System.Security.Cryptography.SHA256.HashData(File.ReadAllBytes(databasePath))));
        await using var backup = NewContext(backupPath!);
        Assert.Equal("Preservado", await backup.Jogos.Select(item => item.Titulo).SingleAsync());
        Assert.NotEqual(databasePath, backupPath);
    }

    [Fact]
    public void NaoCriaBackupQuandoBancoAindaNaoExiste()
    {
        var backupDirectory = Path.Combine(_directory, "Backups");

        var backupPath = LocalDatabase.CriarBackupAntesDeAlterar(
            Path.Combine(_directory, "data", "ausente.db"),
            backupDirectory);

        Assert.Null(backupPath);
        Assert.False(Directory.Exists(backupDirectory));
    }

    [Fact]
    public async Task BackupIncluiAlteracoesAindaPresentesNoWal()
    {
        var databasePath = Path.Combine(_directory, "data", "com-wal.db");
        var backupDirectory = Path.Combine(_directory, "Backups");
        await using var context = NewContext(databasePath);
        await context.Database.EnsureCreatedAsync();
        await context.Database.OpenConnectionAsync();
        await context.Database.ExecuteSqlRawAsync("PRAGMA journal_mode=WAL");
        await context.Database.ExecuteSqlRawAsync("PRAGMA wal_autocheckpoint=0");
        context.Jogos.Add(new Jogo { Titulo = "Persistido no WAL" });
        await context.SaveChangesAsync();

        var backupPath = LocalDatabase.CriarBackupAntesDeAlterar(databasePath, backupDirectory);

        await using var backup = NewContext(backupPath!);
        Assert.Contains(await backup.Jogos.ToListAsync(), item => item.Titulo == "Persistido no WAL");
    }

    [Fact]
    public async Task CopiaDadosExistentesPreservandoRelacionamentos()
    {
        var sourcePath = Path.Combine(_directory, "origem.db");
        var destinationPath = Path.Combine(_directory, "destino.db");
        await using var source = NewContext(sourcePath);
        await using var destination = NewContext(destinationPath);
        await source.Database.EnsureCreatedAsync();
        await destination.Database.EnsureCreatedAsync();

        var channel = new Canal { Id = 7, Nome = "STEAM", PermitePosse = true };
        var game = new Jogo { Id = 11, Titulo = "Jogo preservado", AnoLancamento = 2024 };
        var ownership = new PosseJogo
        {
            Id = 13,
            JogoId = game.Id,
            CanalId = channel.Id,
            Jogo = game,
            Canal = channel
        };
        source.AddRange(channel, game, ownership);
        source.RegistrosJogos.Add(new RegistroJogo
        {
            Id = 17,
            PosseJogoId = ownership.Id,
            PosseJogo = ownership,
            FinalizadoEm = new DateTime(2025, 1, 2),
            TempoEmHoras = 8.5m
        });
        source.PerfisLocais.Add(new PerfilLocal { Id = 1, Nome = "Jogador" });
        await source.SaveChangesAsync();

        await LegacyDatabaseMigration.CopiarAsync(source, destination);

        Assert.Equal(1, await destination.Jogos.CountAsync());
        Assert.Equal(1, await destination.PossesJogos.CountAsync());
        Assert.Equal(1, await destination.RegistrosJogos.CountAsync());
        Assert.Equal(8.5m, await destination.RegistrosJogos.Select(item => item.TempoEmHoras).SingleAsync());
    }

    [Fact]
    public async Task MesclaJogoDoBancoAntigoSemDuplicarOsExistentes()
    {
        var sourcePath = Path.Combine(_directory, "antigo.db");
        var destinationPath = Path.Combine(_directory, "atual.db");
        await using var source = NewContext(sourcePath);
        await using var destination = NewContext(destinationPath);
        await source.Database.EnsureCreatedAsync();
        await destination.Database.EnsureCreatedAsync();

        var sourceChannel = new Canal { Nome = "STEAM", PermitePosse = true };
        var sourceGame = new Jogo { Titulo = "Jogo recuperado" };
        source.PossesJogos.Add(new PosseJogo { Jogo = sourceGame, Canal = sourceChannel });
        destination.Canais.Add(new Canal { Nome = "STEAM", PermitePosse = true });
        destination.Jogos.Add(new Jogo { Titulo = "Jogo já salvo" });
        await source.SaveChangesAsync();
        await destination.SaveChangesAsync();

        await LegacyDatabaseMigration.MesclarAsync(source, destination);
        await LegacyDatabaseMigration.MesclarAsync(source, destination);

        Assert.Equal(2, await destination.Jogos.CountAsync());
        Assert.Equal(1, await destination.Canais.CountAsync());
        Assert.Equal(1, await destination.PossesJogos.CountAsync());
        Assert.Contains(await destination.Jogos.ToListAsync(), item => item.Titulo == "Jogo recuperado");
    }

    [Fact]
    public async Task MesclaAcompanhamentoLegadoCompletoSemDuplicar()
    {
        var sourcePath = Path.Combine(_directory, "acompanhamento-antigo.db");
        var destinationPath = Path.Combine(_directory, "acompanhamento-atual.db");
        await using var source = NewContext(sourcePath);
        await using var destination = NewContext(destinationPath);
        await source.Database.EnsureCreatedAsync();
        await destination.Database.EnsureCreatedAsync();

        var channel = new Canal { Nome = "STEAM", PermitePosse = true };
        var game = new Jogo { Titulo = "Jogo concluído recuperado" };
        var ownership = new PosseJogo { Jogo = game, Canal = channel };
        source.AcompanhamentosJogos.Add(new AcompanhamentoJogo
        {
            Jogo = game,
            Canal = channel,
            PosseJogo = ownership,
            Status = StatusAcompanhamentoJogo.Zerado,
            DataStatus = new DateTime(2026, 11, 1),
            TempoEmHoras = 12.5m,
            ConquistasPossiveis = 30,
            ConquistasObtidas = 20
        });
        await source.SaveChangesAsync();

        await LegacyDatabaseMigration.MesclarAsync(source, destination);
        await LegacyDatabaseMigration.MesclarAsync(source, destination);

        var recovered = await destination.AcompanhamentosJogos
            .Include(item => item.Jogo)
            .Include(item => item.Canal)
            .SingleAsync();
        Assert.Equal("Jogo concluído recuperado", recovered.Jogo.Titulo);
        Assert.Equal("STEAM", recovered.Canal.Nome);
        Assert.Equal(StatusAcompanhamentoJogo.Zerado, recovered.Status);
        Assert.Equal(new DateTime(2026, 11, 1), recovered.DataStatus);
        Assert.Equal(12.5m, recovered.TempoEmHoras);
        Assert.Equal(30, recovered.ConquistasPossiveis);
        Assert.Equal(20, recovered.ConquistasObtidas);
        Assert.NotNull(recovered.PosseJogoId);
    }

    [Fact]
    public async Task MesclaBancoLegadoSemTabelaDeAcompanhamentos()
    {
        var sourcePath = Path.Combine(_directory, "sem-acompanhamentos.db");
        var destinationPath = Path.Combine(_directory, "destino-completo.db");
        await using var source = NewContext(sourcePath);
        await using var destination = NewContext(destinationPath);
        await source.Database.EnsureCreatedAsync();
        await destination.Database.EnsureCreatedAsync();
        source.Jogos.Add(new Jogo { Titulo = "Jogo de esquema antigo" });
        await source.SaveChangesAsync();
        await source.Database.ExecuteSqlRawAsync("DROP TABLE AcompanhamentosJogos");

        await LegacyDatabaseMigration.MesclarAsync(source, destination);

        Assert.Contains(await destination.Jogos.ToListAsync(),
            item => item.Titulo == "Jogo de esquema antigo");
    }

    private static GameLifeDbContext NewContext(string path)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var options = new DbContextOptionsBuilder<GameLifeDbContext>()
            .UseSqlite(LocalDatabase.ObterConnectionString(path))
            .Options;
        return new GameLifeDbContext(options);
    }

    public void Dispose()
    {
        if (Directory.Exists(_directory))
            Directory.Delete(_directory, recursive: true);
    }
}
