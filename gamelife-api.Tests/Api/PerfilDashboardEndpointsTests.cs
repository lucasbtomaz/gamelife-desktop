using System.Net;
using System.Net.Http.Json;

namespace gamelife_api.Tests.Api;

public sealed class PerfilDashboardEndpointsTests : IClassFixture<GameLifeApiFactory>
{
    private readonly HttpClient _client;

    public PerfilDashboardEndpointsTests(GameLifeApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task PerfilInicialExisteEPodeSerAtualizado()
    {
        var inicial = await _client.GetFromJsonAsync<PerfilResponse>("/api/perfil");
        Assert.NotNull(inicial);
        Assert.Equal(1, inicial.Id);
        Assert.Equal("Jogador", inicial.Nome);

        var response = await _client.PutAsJsonAsync("/api/perfil", new
        {
            nome = "Lucas",
            fotoDataUrl = "data:image/png;base64,AQID",
            exibirJogosRecentes = false
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var atualizado = await response.Content.ReadFromJsonAsync<PerfilResponse>();
        Assert.Equal("Lucas", atualizado!.Nome);
        Assert.False(atualizado.ExibirJogosRecentes);
    }

    [Fact]
    public async Task DashboardFuncionaComBancoVazio()
    {
        var dashboard = await _client.GetFromJsonAsync<DashboardResponse>("/api/dashboard/inicio");

        Assert.NotNull(dashboard);
        Assert.False(string.IsNullOrWhiteSpace(dashboard.Perfil.Nome));
        Assert.Equal(0, dashboard.Resumo.TotalJogos);
        Assert.Empty(dashboard.JogosRecentes);
        Assert.Empty(dashboard.Conquistas);
    }

    [Fact]
    public async Task RejeitaFotoMaiorQueDoisMegabytes()
    {
        var foto = "data:image/png;base64," + new string('A', 2_000_001);

        var response = await _client.PutAsJsonAsync("/api/perfil", new
        {
            nome = "Lucas",
            fotoDataUrl = foto,
            exibirJogosRecentes = true
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private sealed record PerfilResponse(int Id, string Nome, string? FotoDataUrl, bool ExibirJogosRecentes);
    private sealed record ResumoResponse(int TotalJogos, int MidiasFisicas, int JogosDigitais, int TotalColecoes);
    private sealed record DashboardResponse(PerfilResponse Perfil, ResumoResponse Resumo, object[] JogosRecentes, object[] Conquistas);
}
