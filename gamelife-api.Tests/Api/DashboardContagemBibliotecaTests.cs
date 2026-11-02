using System.Net.Http.Json;

namespace gamelife_api.Tests.Api;

public sealed class DashboardContagemBibliotecaTests : IClassFixture<GameLifeApiFactory>
{
    private readonly HttpClient _client;
    public DashboardContagemBibliotecaTests(GameLifeApiFactory factory) => _client = factory.CreateClient();

    [Fact]
    public async Task DesejoExcluidoSemPosseNaoContaComoJogoNoInicio()
    {
        var criada = await _client.PostAsJsonAsync("/api/lista-desejos", new { titulo = "Jogo temporário" });
        var desejo = await criada.Content.ReadFromJsonAsync<DesejoCriado>();
        await _client.DeleteAsync($"/api/lista-desejos/{desejo!.Id}");

        var dashboard = await _client.GetFromJsonAsync<DashboardResponse>("/api/dashboard/inicio");

        Assert.Equal(0, dashboard!.Resumo.TotalJogos);
    }

    private sealed record DesejoCriado(int Id);
    private sealed record ResumoResponse(int TotalJogos);
    private sealed record DashboardResponse(ResumoResponse Resumo);
}
