using System.Net;
using System.Net.Http.Json;
using gamelife_api.Context;
using gamelife_api.Controllers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace gamelife_api.Tests.Api;

public sealed class BibliotecaEndpointsTests : IClassFixture<GameLifeApiFactory>
{
    private readonly GameLifeApiFactory _factory;
    private readonly HttpClient _client;

    public BibliotecaEndpointsTests(GameLifeApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task CadastraUmJogoComDuasPlataformasEmUmaOperacao()
    {
        var titulo = $"Multiplataforma {Guid.NewGuid()}";
        var response = await _client.PostAsJsonAsync("/biblioteca", new
        {
            titulo,
            anoLancamento = 2024,
            plataformas = new object[]
            {
                new { plataforma = "STEAM", midiaFisica = false },
                new
                {
                    plataforma = "XBOX",
                    midiaFisica = true,
                    acompanhamento = new
                    {
                        status = "Zerado",
                        dataStatus = "2026-11-10",
                        tempoEmHoras = 18.5m,
                        conquistasPossiveis = 40,
                        conquistasObtidas = 25
                    }
                }
            }
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<JogoBibliotecaResponse>();
        Assert.NotNull(created);
        Assert.Equal(titulo, created.Titulo);
        Assert.Equal(2024, created.AnoLancamento);
        Assert.Equal(2, created.Plataformas.Count);
        Assert.False(created.Plataformas.Single(item => item.Nome == "STEAM").MidiaFisica);
        var xbox = created.Plataformas.Single(item => item.Nome == "XBOX");
        Assert.True(xbox.MidiaFisica);
        Assert.Equal("Zerado", xbox.Acompanhamento!.Status);
        Assert.Equal(18.5m, xbox.Acompanhamento.TempoEmHoras);

        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<GameLifeDbContext>();
        Assert.Equal(1, await context.Jogos.CountAsync(item => item.Titulo == titulo));
        var jogoId = await context.Jogos.Where(item => item.Titulo == titulo).Select(item => item.Id).SingleAsync();
        Assert.Equal(2, await context.PossesJogos.CountAsync(item => item.JogoId == jogoId));
        Assert.Equal(1, await context.AcompanhamentosJogos.CountAsync(item => item.JogoId == jogoId));
    }

    [Fact]
    public async Task ConsultaRetornaUmaFichaComDuasPlataformas()
    {
        var titulo = $"Ficha única {Guid.NewGuid()}";
        await Cadastrar(titulo,
            new { plataforma = "PC", midiaFisica = false },
            new { plataforma = "PlayStation 5", midiaFisica = true });

        var games = await _client.GetFromJsonAsync<List<JogoBibliotecaResponse>>("/biblioteca");
        var game = Assert.Single(games!, item => item.Titulo == titulo);

        Assert.Equal(2, game.Plataformas.Count);
        Assert.Contains(game.Plataformas, item => item.Nome == "PC");
        Assert.Contains(game.Plataformas, item => item.Nome == "PlayStation 5");
    }

    [Fact]
    public async Task AtualizaPlataformaExistenteSemDuplicarEsemApagarAsDemais()
    {
        var titulo = $"Atualização {Guid.NewGuid()}";
        await Cadastrar(titulo,
            new { plataforma = "PC", midiaFisica = false },
            new { plataforma = "STEAM", midiaFisica = false });

        var response = await Cadastrar(titulo, new { plataforma = " pc ", midiaFisica = true });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var games = await _client.GetFromJsonAsync<List<JogoBibliotecaResponse>>("/biblioteca");
        var game = Assert.Single(games!, item => item.Titulo == titulo);
        Assert.Equal(2, game.Plataformas.Count);
        Assert.True(game.Plataformas.Single(item => item.Nome == "PC").MidiaFisica);
        Assert.Contains(game.Plataformas, item => item.Nome == "STEAM");
    }

    [Fact]
    public async Task ReutilizaFichaQuandoOTituloMudaApenasEmCaixaEspacosOuPontuacao()
    {
        var identificador = Guid.NewGuid().ToString("N");
        var titulo = $"Jogo: Especial {identificador}";
        await Cadastrar(titulo, new { plataforma = "PC", midiaFisica = false });

        var response = await Cadastrar(
            $"  jogo especial {identificador.ToUpperInvariant()}  ",
            new { plataforma = "STEAM", midiaFisica = false });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var games = await _client.GetFromJsonAsync<List<JogoBibliotecaResponse>>("/biblioteca");
        var game = Assert.Single(games!, item => item.Titulo == titulo);
        Assert.Equal(2, game.Plataformas.Count);
    }

    [Fact]
    public async Task OmitirAcompanhamentoPreservaOExistente()
    {
        var titulo = $"Progresso preservado {Guid.NewGuid()}";
        await _client.PostAsJsonAsync("/biblioteca", new
        {
            titulo,
            plataformas = new[]
            {
                new
                {
                    plataforma = "PC",
                    midiaFisica = false,
                    acompanhamento = new
                    {
                        status = "NaoTerminado",
                        tempoEmHoras = 7.5m,
                        conquistasPossiveis = 10,
                        conquistasObtidas = 3
                    }
                }
            }
        });

        var response = await Cadastrar(titulo, new { plataforma = "PC", midiaFisica = true });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var games = await _client.GetFromJsonAsync<List<JogoBibliotecaResponse>>("/biblioteca");
        var platform = Assert.Single(Assert.Single(games!, item => item.Titulo == titulo).Plataformas);
        Assert.True(platform.MidiaFisica);
        Assert.Equal("NaoTerminado", platform.Acompanhamento!.Status);
        Assert.Equal(7.5m, platform.Acompanhamento.TempoEmHoras);
    }

    [Fact]
    public async Task PlataformaRepetidaReverteTodaAOperacao()
    {
        var titulo = $"Pedido inválido {Guid.NewGuid()}";
        var response = await Cadastrar(titulo,
            new { plataforma = "PC", midiaFisica = false },
            new { plataforma = " pc ", midiaFisica = true });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemResponse>();
        Assert.Contains("mais de uma vez", problem!.Detail, StringComparison.OrdinalIgnoreCase);
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<GameLifeDbContext>();
        Assert.False(await context.Jogos.AnyAsync(item => item.Titulo == titulo));
    }

    [Fact]
    public void ProjecaoPlanaDaBibliotecaContinuaTraduzivelPeloMySql()
    {
        var options = new DbContextOptionsBuilder<GameLifeDbContext>()
            .UseMySQL("Server=localhost;Database=teste;User=teste;Password=teste")
            .Options;
        using var context = new GameLifeDbContext(options);

        var sql = BibliotecaController.CriarConsulta(context).ToQueryString();

        Assert.Contains("ORDER BY", sql, StringComparison.OrdinalIgnoreCase);
    }

    private Task<HttpResponseMessage> Cadastrar(string titulo, params object[] plataformas) =>
        _client.PostAsJsonAsync("/biblioteca", new { titulo, plataformas });

    private sealed record JogoBibliotecaResponse(
        int JogoId,
        string Titulo,
        int? AnoLancamento,
        List<PlataformaBibliotecaResponse> Plataformas);

    private sealed record PlataformaBibliotecaResponse(
        int PosseId,
        int CanalId,
        string Nome,
        bool MidiaFisica,
        AcompanhamentoBibliotecaResponse? Acompanhamento);

    private sealed record AcompanhamentoBibliotecaResponse(
        int Id,
        string Status,
        DateTime? DataStatus,
        decimal? TempoEmHoras,
        int? ConquistasPossiveis,
        int? ConquistasObtidas);

    private sealed record ProblemResponse(string Detail);
}
