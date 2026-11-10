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
