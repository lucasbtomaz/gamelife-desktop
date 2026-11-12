using gamelife_api.Context;
using gamelife_api.Contracts;
using gamelife_api.Controllers;
using gamelife_api.Models;
using Microsoft.EntityFrameworkCore;

namespace gamelife_api.Tests.Api;

public sealed class ListaDesejosSqliteTests
{
    [Fact]
    public async Task AdicionarPrecoReutilizaCanalAcentuadoNoSqlite()
    {
        var diretorio = Path.Combine(Path.GetTempPath(), $"GameLifeDesejosSqlite-{Guid.NewGuid():N}");
        Directory.CreateDirectory(diretorio);
        var banco = Path.Combine(diretorio, "gamelife.db");

        try
        {
            var opcoes = new DbContextOptionsBuilder<GameLifeDbContext>()
                .UseSqlite($"Data Source={banco};Foreign Keys=True;Pooling=False")
                .Options;

            await using (var contexto = new GameLifeDbContext(opcoes))
            {
                await contexto.Database.EnsureCreatedAsync();
                var canal = new Canal { Nome = "LOJA ÉPICA", PermitePreco = false };
                var desejo = new ItemListaDesejos
                {
                    Jogo = new Jogo { Titulo = "Jogo acentuado" },
                    CriadoEm = DateTime.UtcNow
                };
                contexto.AddRange(canal, desejo);
                await contexto.SaveChangesAsync();

                var resposta = await new ItensListaDesejosController(contexto).AdicionarPreco(
                    desejo.Id,
                    new AdicionarPrecoDesejoRequest(null, "LOJA ÉPICA", 49.90m, 99.90m, true));

                Assert.NotNull(resposta.Result);
                contexto.ChangeTracker.Clear();
                Assert.Equal(1, await contexto.Canais.CountAsync());
                Assert.False(await contexto.Canais.Select(item => item.PermitePreco).SingleAsync());
                Assert.Equal(canal.Id, await contexto.RegistrosPrecos.Select(item => item.CanalId).SingleAsync());
            }
        }
        finally
        {
            Directory.Delete(diretorio, recursive: true);
        }
    }
}
