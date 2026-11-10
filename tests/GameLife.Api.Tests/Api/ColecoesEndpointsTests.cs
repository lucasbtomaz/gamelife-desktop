using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace gamelife_api.Tests.Api;

public sealed class ColecoesEndpointsTests : IClassFixture<GameLifeApiFactory>
{
    private readonly HttpClient _client;

    public ColecoesEndpointsTests(GameLifeApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task CriarAssociarEExcluirColecaoPreservaJogo()
    {
        await _client.PostAsJsonAsync("/biblioteca", new
        {
            titulo = "Hades",
            plataformas = new[] { new { plataforma = "PC", midiaFisica = false } }
        });
        var jogos = await _client.GetFromJsonAsync<JsonElement[]>("/api/jogos?search=Hades");
        var jogoId = jogos![0].GetProperty("id").GetInt32();

        var criar = await _client.PostAsJsonAsync("/api/colecoes", new
        {
            nome = "Favoritos",
            descricao = "Jogos para revisitar"
        });
        Assert.Equal(HttpStatusCode.Created, criar.StatusCode);
        var colecao = await criar.Content.ReadFromJsonAsync<ColecaoResponse>();

        var associar = await _client.PostAsJsonAsync($"/api/colecoes/{colecao!.Id}/jogos", new { jogoId });
        Assert.Equal(HttpStatusCode.NoContent, associar.StatusCode);

        var duplicar = await _client.PostAsJsonAsync($"/api/colecoes/{colecao.Id}/jogos", new { jogoId });
        Assert.Equal(HttpStatusCode.Conflict, duplicar.StatusCode);

        var colecoes = await _client.GetFromJsonAsync<ColecaoResponse[]>("/api/colecoes");
        Assert.Single(colecoes!);
        Assert.Single(colecoes[0].Jogos);
        Assert.Equal("Hades", colecoes[0].Jogos[0].Titulo);

        var excluir = await _client.DeleteAsync($"/api/colecoes/{colecao.Id}");
        Assert.Equal(HttpStatusCode.NoContent, excluir.StatusCode);
        var jogo = await _client.GetAsync($"/api/jogos/{jogoId}");
        Assert.Equal(HttpStatusCode.OK, jogo.StatusCode);
    }

    private sealed record JogoColecaoResponse(int Id, string Titulo);
    private sealed record ColecaoResponse(int Id, string Nome, string? Descricao, JogoColecaoResponse[] Jogos);
}
