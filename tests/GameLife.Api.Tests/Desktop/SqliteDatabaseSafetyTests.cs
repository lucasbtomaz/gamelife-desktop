using gamelife_api.Context;
using gamelife_api.Models;
using gamelife_api.Persistence;
using GameLife.Desktop;
using Microsoft.EntityFrameworkCore;

namespace gamelife_api.Tests.Desktop;

public sealed class SqliteDatabaseSafetyTests : IDisposable
{
    private readonly string _directory = Path.Combine(
        Path.GetTempPath(), $"GameLifeDatabaseSafety-{Guid.NewGuid():N}");

    [Fact]
    public async Task CapturaIntegridadeEContagensSemAlterarBanco()
    {
        var path = Path.Combine(_directory, "gamelife.db");
        await using (var context = NewContext(path))
        {
            await context.Database.EnsureCreatedAsync();
            context.Jogos.AddRange(new Jogo { Titulo = "Um" }, new Jogo { Titulo = "Dois" });
            await context.SaveChangesAsync();
        }
        var before = File.ReadAllBytes(path);

        var snapshot = await SqliteDatabaseSafety.CapturarAsync(path);

        Assert.Equal("ok", snapshot.Integridade);
        Assert.Equal(2, snapshot.Contagens["Jogos"]);
        Assert.Equal(before, File.ReadAllBytes(path));
    }

    [Fact]
    public void RejeitaReducaoDeQualquerTabela()
    {
        var before = new SqliteDatabaseSnapshot("ok", 100, "A", new Dictionary<string, long>
        {
            ["Jogos"] = 2,
            ["PossesJogos"] = 2
        });
        var after = new SqliteDatabaseSnapshot("ok", 90, "B", new Dictionary<string, long>
        {
            ["Jogos"] = 2,
            ["PossesJogos"] = 1
        });

        var exception = Assert.Throws<InvalidOperationException>(() =>
            SqliteDatabaseSafety.ValidarPreservacao(before, after));

        Assert.Contains("PossesJogos", exception.Message);
    }

    private static GameLifeDbContext NewContext(string path)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        return new GameLifeDbContext(new DbContextOptionsBuilder<GameLifeDbContext>()
            .UseSqlite(LocalDatabase.ObterConnectionString(path))
            .Options);
    }

    public void Dispose()
    {
        if (Directory.Exists(_directory))
            Directory.Delete(_directory, recursive: true);
    }
}
