using gamelife_api.Context;
using gamelife_api.Contracts;
using gamelife_api.Models;
using gamelife_api.Services;
using Microsoft.EntityFrameworkCore;

namespace gamelife_api.Tests.Api;

public sealed class BibliotecaSqliteTests
{
    [Fact]
    public async Task SalvarRetornaFichaDepoisDeGravarNovaPlataformaNoSqlite()
    {
        var directory = Path.Combine(
            Path.GetTempPath(), $"GameLifeBibliotecaSqlite-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        var databasePath = Path.Combine(directory, "gamelife.db");

        try
        {
            var options = new DbContextOptionsBuilder<GameLifeDbContext>()
                .UseSqlite($"Data Source={databasePath};Foreign Keys=True;Pooling=False")
                .Options;
            await using var context = new GameLifeDbContext(options);
            await context.Database.EnsureCreatedAsync();

            var game = new Jogo { Titulo = "Graveyard Keeper" };
            var steam = new Canal { Nome = "Steam", PermitePosse = true };
            context.AddRange(game, steam);
            context.PossesJogos.Add(new PosseJogo
            {
                Jogo = game,
                Canal = steam,
                MidiaFisica = false
            });
            await context.SaveChangesAsync();

            var service = new BibliotecaService(context);
            var result = await service.SalvarAsync(
                new SalvarJogoBibliotecaRequest(
                    "Graveyard Keeper",
                    null,
                    [
                        new SalvarPlataformaBibliotecaRequest("STEAM", false, null),
                        new SalvarPlataformaBibliotecaRequest("EPIC", false, null)
                    ]),
                CancellationToken.None);

            Assert.False(result.Criado);
            Assert.Equal(2, result.Jogo.Plataformas.Count);
            Assert.Contains(result.Jogo.Plataformas, item => item.Nome == "Steam");
            Assert.Contains(result.Jogo.Plataformas, item => item.Nome == "EPIC");
            Assert.Equal(2, await context.PossesJogos.CountAsync(item => item.JogoId == game.Id));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }
}
