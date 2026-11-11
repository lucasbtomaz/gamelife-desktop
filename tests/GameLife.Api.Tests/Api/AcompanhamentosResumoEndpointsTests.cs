using System.Net.Http.Json;

namespace gamelife_api.Tests.Api;

public sealed class AcompanhamentosResumoEndpointsTests : IClassFixture<GameLifeApiFactory>
{
    private readonly HttpClient _client;

    public AcompanhamentosResumoEndpointsTests(GameLifeApiFactory factory) => _client = factory.CreateClient();

    [Fact]
    public async Task IncluiJogosDaBibliotecaSemAcompanhamentoComoNaoTerminados()
    {
        await AdicionarBiblioteca("Biblioteca zerado", "PC");
        await AdicionarBiblioteca("Biblioteca sem registro", "STEAM");
        await Acompanhar("Biblioteca zerado", "PC", "Zerado");
        await Acompanhar("Assinatura dropada", "PlayStation 5", "Dropado");
        await Acompanhar("Assinatura em andamento", "Xbox Series X|S", "NaoTerminado");

        var resumo = await _client.GetFromJsonAsync<ResumoResponse>("/api/acompanhamentos-jogos/resumo");

        Assert.NotNull(resumo);
        Assert.Equal(4, resumo.Total);
        Assert.Equal(1, resumo.Zerados);
        Assert.Equal(1, resumo.Dropados);
        Assert.Equal(2, resumo.NaoTerminados);
    }

    [Fact]
    public async Task ContaFichaMultiplataformaComoUmUnicoJogo()
    {
        var anterior = await _client.GetFromJsonAsync<ResumoResponse>("/api/acompanhamentos-jogos/resumo");
        Assert.NotNull(anterior);
        var titulo = $"Multiplataforma {Guid.NewGuid()}";
        var resposta = await _client.PostAsJsonAsync("/biblioteca", new
        {
            titulo,
            plataformas = new[]
            {
                new { plataforma = "STEAM", midiaFisica = false },
                new { plataforma = "XBOX", midiaFisica = false },
                new { plataforma = "PlayStation 5", midiaFisica = false }
            }
        });
        resposta.EnsureSuccessStatusCode();

        var resumo = await _client.GetFromJsonAsync<ResumoResponse>("/api/acompanhamentos-jogos/resumo");

        Assert.NotNull(resumo);
        Assert.Equal(anterior.Total + 1, resumo.Total);
        Assert.Equal(anterior.Zerados, resumo.Zerados);
        Assert.Equal(anterior.Dropados, resumo.Dropados);
        Assert.Equal(anterior.NaoTerminados + 1, resumo.NaoTerminados);
    }

    private async Task AdicionarBiblioteca(string titulo, string plataforma)
    {
        var resposta = await _client.PostAsJsonAsync("/biblioteca", new
        {
            titulo,
            plataformas = new[] { new { plataforma, midiaFisica = false } }
        });
        resposta.EnsureSuccessStatusCode();
    }

    private async Task Acompanhar(string titulo, string plataforma, string status)
    {
        var resposta = await _client.PostAsJsonAsync("/api/acompanhamentos-jogos", new
        {
            titulo, plataforma, status, conquistasPossiveis = 10, conquistasObtidas = 5
        });
        resposta.EnsureSuccessStatusCode();
    }

    private sealed record ResumoResponse(int Total, int Zerados, int Dropados, int NaoTerminados);
}
