using System.Net;
using System.Net.Http.Json;
using gamelife_api.Controllers;

namespace gamelife_api.Tests.Api;

public sealed class AcompanhamentosJogosEndpointsTests : IClassFixture<GameLifeApiFactory>
{
    private readonly HttpClient _client;

    public AcompanhamentosJogosEndpointsTests(GameLifeApiFactory factory) => _client = factory.CreateClient();

    [Fact]
    public async Task CadastraJogoDeAssinaturaSemIncluiLoNaBiblioteca()
    {
        var titulo = $"Assinatura {Guid.NewGuid()}";
        var resposta = await _client.PostAsJsonAsync("/api/acompanhamentos-jogos", Novo(titulo));

        Assert.Equal(HttpStatusCode.Created, resposta.StatusCode);
        var criado = await resposta.Content.ReadFromJsonAsync<AcompanhamentoResponse>();
        Assert.NotNull(criado);
        Assert.False(criado.EstaNaBiblioteca);
        Assert.Equal(50, criado.PercentualConquistas);
        var biblioteca = await _client.GetFromJsonAsync<List<JogoBibliotecaResponse>>("/biblioteca");
        Assert.DoesNotContain(biblioteca!, item => item.Titulo == titulo);
    }

    [Fact]
    public async Task IncluiNaBibliotecaEReutilizaPosseExistente()
    {
        var titulo = $"Biblioteca {Guid.NewGuid()}";
        await _client.PostAsJsonAsync("/biblioteca", NovoItemBiblioteca(titulo, "PC", true));

        var resposta = await _client.PostAsJsonAsync("/api/acompanhamentos-jogos", Novo(titulo, adicionarBiblioteca: true));

        Assert.Equal(HttpStatusCode.Created, resposta.StatusCode);
        var criado = await resposta.Content.ReadFromJsonAsync<AcompanhamentoResponse>();
        Assert.True(criado!.EstaNaBiblioteca);
        var biblioteca = await _client.GetFromJsonAsync<List<JogoBibliotecaResponse>>("/biblioteca");
        var jogo = Assert.Single(biblioteca!, item => item.Titulo == titulo);
        Assert.Single(jogo.Plataformas, plataforma => plataforma.Nome == "PC");
    }

    [Fact]
    public async Task PermiteMesmoJogoEmPlataformasDiferentesEMasNaoNaMesma()
    {
        var titulo = $"Multiplataforma {Guid.NewGuid()}";
        Assert.Equal(HttpStatusCode.Created, (await _client.PostAsJsonAsync("/api/acompanhamentos-jogos", Novo(titulo))).StatusCode);
        Assert.Equal(HttpStatusCode.Created, (await _client.PostAsJsonAsync("/api/acompanhamentos-jogos", Novo(titulo, "PlayStation 5"))).StatusCode);

        var duplicado = await _client.PostAsJsonAsync("/api/acompanhamentos-jogos", Novo($"  {titulo.ToUpperInvariant()}  ", " pc "));

        Assert.Equal(HttpStatusCode.Conflict, duplicado.StatusCode);
    }

    [Theory]
    [InlineData(null, 1)]
    [InlineData(2, 3)]
    public async Task RejeitaConquistasObtidasInvalidas(int? possiveis, int obtidas)
    {
        var resposta = await _client.PostAsJsonAsync("/api/acompanhamentos-jogos", new
        {
            titulo = $"Inválido {Guid.NewGuid()}", plataforma = "PC", status = "Zerado",
            conquistasPossiveis = possiveis, conquistasObtidas = obtidas
        });

        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
    }

    [Fact]
    public async Task EditaExcluiEResumeSemApagarAPosse()
    {
        var titulo = $"Editável {Guid.NewGuid()}";
        var cadastro = await _client.PostAsJsonAsync("/api/acompanhamentos-jogos", Novo(titulo, adicionarBiblioteca: true));
        var criado = await cadastro.Content.ReadFromJsonAsync<AcompanhamentoResponse>();

        var edicao = await _client.PutAsJsonAsync($"/api/acompanhamentos-jogos/{criado!.Id}", new
        {
            titulo, plataforma = "PC", status = "Dropado", tempoEmHoras = 21.5m,
            conquistasPossiveis = 20, conquistasObtidas = 5, adicionarBiblioteca = false
        });
        Assert.Equal(HttpStatusCode.OK, edicao.StatusCode);
        var atualizado = await edicao.Content.ReadFromJsonAsync<AcompanhamentoResponse>();
        Assert.Equal("Dropado", atualizado!.Status);
        Assert.True(atualizado.EstaNaBiblioteca);
        Assert.Equal(25, atualizado.PercentualConquistas);

        var resumo = await _client.GetFromJsonAsync<ResumoResponse>("/api/acompanhamentos-jogos/resumo");
        Assert.True(resumo!.Total >= 1);
        Assert.True(resumo.Dropados >= 1);

        Assert.Equal(HttpStatusCode.NoContent, (await _client.DeleteAsync($"/api/acompanhamentos-jogos/{criado.Id}")).StatusCode);
        var biblioteca = await _client.GetFromJsonAsync<List<JogoBibliotecaResponse>>("/biblioteca");
        Assert.Contains(biblioteca!, item => item.Titulo == titulo);
    }

    [Fact]
    public void NormalizaAcentosCaixaEEspacosComAMesmaChave()
    {
        Assert.Equal(
            AcompanhamentosJogosController.NormalizarChave("  Éclipse  "),
            AcompanhamentosJogosController.NormalizarChave("éCLIPSE"));
    }

    [Fact]
    public async Task RejeitaStatusNumericoForaDosValoresPermitidos()
    {
        var resposta = await _client.PostAsJsonAsync("/api/acompanhamentos-jogos", new
        {
            titulo = $"Status inválido {Guid.NewGuid()}", plataforma = "PC", status = "999"
        });

        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
    }

    [Fact]
    public async Task ConflitoNaoAlteraCanalNemPersisteReferenciasParciais()
    {
        var titulo = $"Sem efeito parcial {Guid.NewGuid()}";
        var plataforma = $"Canal {Guid.NewGuid()}";
        await _client.PostAsJsonAsync("/api/acompanhamentos-jogos", Novo(titulo, plataforma));

        var duplicado = await _client.PostAsJsonAsync("/api/acompanhamentos-jogos", Novo(titulo, plataforma, adicionarBiblioteca: true));

        Assert.Equal(HttpStatusCode.Conflict, duplicado.StatusCode);
        var canais = await _client.GetFromJsonAsync<List<CanalResponse>>("/api/canais");
        Assert.False(canais!.Single(item => item.Nome == plataforma).PermitePosse);
    }

    [Fact]
    public async Task ReconhecePosseAdicionadaDepoisEDesvinculaAoExcluiLa()
    {
        var titulo = $"Posse posterior {Guid.NewGuid()}";
        await _client.PostAsJsonAsync("/api/acompanhamentos-jogos", Novo(titulo));
        await _client.PostAsJsonAsync("/biblioteca", NovoItemBiblioteca(titulo));

        var acompanhamentos = await _client.GetFromJsonAsync<List<AcompanhamentoResponse>>("/api/acompanhamentos-jogos");
        Assert.True(acompanhamentos!.Single(item => item.Titulo == titulo).EstaNaBiblioteca);
        var biblioteca = await _client.GetFromJsonAsync<List<JogoBibliotecaResponse>>("/biblioteca");
        var posse = biblioteca!.Single(item => item.Titulo == titulo).Plataformas.Single(item => item.Nome == "PC");

        Assert.Equal(HttpStatusCode.NoContent, (await _client.DeleteAsync($"/api/posses/{posse.PosseId}")).StatusCode);
        acompanhamentos = await _client.GetFromJsonAsync<List<AcompanhamentoResponse>>("/api/acompanhamentos-jogos");
        Assert.False(acompanhamentos!.Single(item => item.Titulo == titulo).EstaNaBiblioteca);
    }

    [Fact]
    public async Task BloqueiaExclusaoDeJogoOuCanalAindaAcompanhado()
    {
        var titulo = $"Referências protegidas {Guid.NewGuid()}";
        var resposta = await _client.PostAsJsonAsync("/api/acompanhamentos-jogos", Novo(titulo, "Outra"));
        var criado = await resposta.Content.ReadFromJsonAsync<AcompanhamentoResponse>();

        Assert.Equal(HttpStatusCode.Conflict, (await _client.DeleteAsync($"/api/jogos/{criado!.JogoId}")).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await _client.DeleteAsync($"/api/canais/{criado.CanalId}")).StatusCode);
    }

    private static object Novo(string titulo, string plataforma = "PC", bool adicionarBiblioteca = false) => new
    {
        titulo, plataforma, status = "Zerado", dataStatus = "2026-11-05",
        tempoEmHoras = 10.5m, conquistasPossiveis = 20, conquistasObtidas = 10, adicionarBiblioteca
    };

    private static object NovoItemBiblioteca(string titulo, string plataforma = "PC", bool midiaFisica = false) => new
    {
        titulo,
        plataformas = new[] { new { plataforma, midiaFisica } }
    };

    private sealed record AcompanhamentoResponse(int Id, int JogoId, string Titulo, int CanalId, string Plataforma, string Status, decimal PercentualConquistas, bool EstaNaBiblioteca);
    private sealed record ResumoResponse(int Total, int Zerados, int Dropados, int NaoTerminados);
    private sealed record JogoBibliotecaResponse(int JogoId, string Titulo, PlataformaBibliotecaResponse[] Plataformas);
    private sealed record PlataformaBibliotecaResponse(int PosseId, string Nome, bool MidiaFisica);
    private sealed record CanalResponse(int Id, string Nome, bool PermitePosse);
}
