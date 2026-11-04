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
