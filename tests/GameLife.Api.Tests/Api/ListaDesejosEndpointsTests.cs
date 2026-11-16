using System.Net;
using System.Net.Http.Json;

namespace gamelife_api.Tests.Api;

public sealed class ListaDesejosEndpointsTests : IClassFixture<GameLifeApiFactory>
{
    private readonly HttpClient _client;

    public ListaDesejosEndpointsTests(GameLifeApiFactory factory) => _client = factory.CreateClient();

    [Fact]
    public async Task CriarDesejoReutilizaJogoDaBibliotecaESinalizaPosse()
    {
        await _client.PostAsJsonAsync("/biblioteca", NovoItemBiblioteca("Hades", "PC"));

        var resposta = await _client.PostAsJsonAsync("/api/lista-desejos", new
        {
            titulo = "  HADES  ", precoDesejado = 45.90m, observacao = "Jogar nas férias"
        });

        Assert.Equal(HttpStatusCode.Created, resposta.StatusCode);
        var item = await resposta.Content.ReadFromJsonAsync<DesejoResponse>();
        Assert.Equal("Hades", item!.Titulo);
        Assert.True(item.JaTenho);
        Assert.Equal(45.90m, item.PrecoDesejado);
    }

    [Fact]
    public async Task CriarDesejoNormalizaTituloEImpedeDuplicidade()
    {
        var primeira = await _client.PostAsJsonAsync("/api/lista-desejos", new { titulo = "Sea of Stars" });
        var repetida = await _client.PostAsJsonAsync("/api/lista-desejos", new { titulo = " sea OF stars " });

        Assert.Equal(HttpStatusCode.Created, primeira.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, repetida.StatusCode);
    }

    [Fact]
    public async Task AdicionarPrecoMantemHistoricoEDestacaMenorValor()
    {
        var criada = await _client.PostAsJsonAsync("/api/lista-desejos", new { titulo = "Silksong" });
        var desejo = await criada.Content.ReadFromJsonAsync<DesejoResponse>();

        foreach (var valor in new[] { 99.90m, 59.90m, 79.90m })
        {
            var preco = await _client.PostAsJsonAsync($"/api/lista-desejos/{desejo!.Id}/precos", new
            {
                nomeCanal = "Steam", precoAtual = valor, precoCheio = 99.90m, disponivel = true
            });
            Assert.Equal(HttpStatusCode.Created, preco.StatusCode);
        }

        var itens = await _client.GetFromJsonAsync<DesejoResponse[]>("/api/lista-desejos");
        var item = Assert.Single(itens!, x => x.Id == desejo!.Id);
        Assert.Equal(3, item.HistoricoPrecos.Length);
        Assert.Equal(79.90m, Assert.Single(item.PrecosAtuais).PrecoAtual);
        Assert.Equal(59.90m, item.MenorPrecoHistorico);
        Assert.Equal(79.90m, item.MenorPrecoAtual);
    }

    [Fact]
    public async Task ExcluirDesejoPreservaJogoDaBiblioteca()
    {
        await _client.PostAsJsonAsync("/biblioteca", NovoItemBiblioteca("Celeste", "PC"));
        var criada = await _client.PostAsJsonAsync("/api/lista-desejos", new { titulo = "Celeste" });
        var desejo = await criada.Content.ReadFromJsonAsync<DesejoResponse>();

        Assert.Equal(HttpStatusCode.NoContent, (await _client.DeleteAsync($"/api/lista-desejos/{desejo!.Id}")).StatusCode);
        var biblioteca = await _client.GetFromJsonAsync<BibliotecaItem[]>("/biblioteca");
        Assert.Contains(biblioteca!, x => x.Titulo == "Celeste");
    }

    [Fact]
    public async Task AdicionarNaBibliotecaDepoisDoDesejoAtualizaFlagDePosse()
    {
        await _client.PostAsJsonAsync("/api/lista-desejos", new { titulo = "Neva" });
        await _client.PostAsJsonAsync("/biblioteca", NovoItemBiblioteca(" neVA ", "STEAM"));

        var itens = await _client.GetFromJsonAsync<DesejoResponse[]>("/api/lista-desejos");
        Assert.True(Assert.Single(itens!, x => x.Titulo == "Neva").JaTenho);
    }

    [Fact]
    public async Task PrecoIndisponivelNaoParticipaDoMenorAtual()
    {
        var criada = await _client.PostAsJsonAsync("/api/lista-desejos", new { titulo = "Nine Sols" });
        var desejo = await criada.Content.ReadFromJsonAsync<DesejoResponse>();
        await _client.PostAsJsonAsync($"/api/lista-desejos/{desejo!.Id}/precos", new { nomeCanal = "STEAM", precoAtual = 50m, disponivel = true });
        await _client.PostAsJsonAsync($"/api/lista-desejos/{desejo.Id}/precos", new { nomeCanal = "EPIC", precoAtual = 10m, disponivel = false });

        var itens = await _client.GetFromJsonAsync<DesejoResponse[]>("/api/lista-desejos");
        Assert.Equal(50m, Assert.Single(itens!, x => x.Id == desejo.Id).MenorPrecoAtual);
    }

    private sealed record BibliotecaItem(string Titulo);

    private static object NovoItemBiblioteca(string titulo, string plataforma) => new
    {
        titulo,
        plataformas = new[] { new { plataforma, midiaFisica = false } }
    };

    private sealed record PrecoAtualResponse(int CanalId, string CanalNome, decimal PrecoAtual, decimal? MenorPrecoHistorico);
    private sealed record HistoricoPrecoResponse(int Id, decimal PrecoAtual);
    private sealed record DesejoResponse(
        int Id, int JogoId, string Titulo, bool JaTenho, decimal? PrecoDesejado,
        PrecoAtualResponse[] PrecosAtuais, decimal? MenorPrecoAtual,
        decimal? MenorPrecoHistorico, HistoricoPrecoResponse[] HistoricoPrecos);
}
