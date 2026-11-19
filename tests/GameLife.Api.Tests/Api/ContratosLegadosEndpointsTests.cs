using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using gamelife_api.Context;
using gamelife_api.Contracts;
using gamelife_api.Models;
using Microsoft.Extensions.DependencyInjection;

namespace gamelife_api.Tests.Api;

public sealed class ContratosLegadosEndpointsTests(GameLifeApiFactory factory) : IClassFixture<GameLifeApiFactory>
{
    [Fact]
    public async Task CriacaoDeJogoIgnoraIdENavegacoesDoCliente()
    {
        using var client = factory.CreateClient();
        var titulo = $"Jogo {Guid.NewGuid()}";
        var resposta = await client.PostAsJsonAsync("/api/jogos", new
        {
            id = int.MaxValue, titulo, anoLancamento = 2025,
            possesJogos = new[] { new { canalId = 1 } }
        });
        Assert.Equal(HttpStatusCode.Created, resposta.StatusCode);
        var jogo = await resposta.Content.ReadFromJsonAsync<JsonElement>();
        Assert.NotEqual(int.MaxValue, jogo.GetProperty("id").GetInt32());
        Assert.Equal(titulo, jogo.GetProperty("titulo").GetString());
        Assert.False(jogo.TryGetProperty("possesJogos", out _));
    }

    [Fact]
    public async Task RotasDeCadastroValidamEAceitamApenasCamposDosContratos()
    {
        using var client = factory.CreateClient();
        var titulo = $"Jogo {Guid.NewGuid()}";
        var criado = await client.PostAsJsonAsync("/api/jogos", new { titulo });
        Assert.Equal(HttpStatusCode.Created, criado.StatusCode);
        var jogo = (await criado.Content.ReadFromJsonAsync<JogoResponse>())!;
        Assert.Equal(HttpStatusCode.BadRequest,
            (await client.PutAsJsonAsync($"/api/jogos/{jogo.Id}", new { titulo, anoLancamento = -1 })).StatusCode);

        criado = await client.PostAsJsonAsync("/api/canais", new
        {
            id = int.MaxValue, nome = titulo, permitePosse = true, permitePreco = true
        });
        Assert.Equal(HttpStatusCode.Created, criado.StatusCode);
        var canal = (await criado.Content.ReadFromJsonAsync<CanalResponse>())!;
        Assert.NotEqual(int.MaxValue, canal.Id);

        criado = await client.PostAsJsonAsync("/api/posses", new { jogoId = jogo.Id, canalId = canal.Id });
        Assert.Equal(HttpStatusCode.Created, criado.StatusCode);
        Assert.Equal(jogo.Id, (await criado.Content.ReadFromJsonAsync<PosseCriadaResponse>())!.JogoId);

        criado = await client.PostAsJsonAsync("/api/lista-desejos", new { titulo });
        Assert.Equal(HttpStatusCode.Created, criado.StatusCode);
        var desejoId = (await criado.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt32();

        var preco = new { itemListaDesejosId = desejoId, canalId = canal.Id, precoCheio = 15, precoAtual = -1 };
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/precos", preco)).StatusCode);
        criado = await client.PostAsJsonAsync("/api/precos", preco with { precoAtual = 10 });
        Assert.Equal(HttpStatusCode.OK, criado.StatusCode);
        Assert.Equal(10, (await criado.Content.ReadFromJsonAsync<PrecoResponse>())!.PrecoAtual);

        criado = await client.PostAsJsonAsync("/api/motivos", new { itemListaDesejosId = desejoId, descricao = "Recomendado" });
        Assert.Equal(HttpStatusCode.Created, criado.StatusCode);
        Assert.Equal(desejoId, (await criado.Content.ReadFromJsonAsync<MotivoResponse>())!.ItemListaDesejosId);
        var detalhe = await client.GetFromJsonAsync<JogoDetalheResponse>($"/api/jogos/{jogo.Id}");
        Assert.Equal(canal.Nome, Assert.Single(detalhe!.Posses).CanalNome);
        Assert.Equal("Recomendado", Assert.Single(detalhe.ItemListaDesejos!.Motivos).Descricao);
    }

    [Fact]
    public async Task LegadoPodeSerConsultadoSemContarNoResumoDeZerados()
    {
        using var client = factory.CreateClient();
        int id;
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var contexto = scope.ServiceProvider.GetRequiredService<GameLifeDbContext>();
            var registro = new RegistroJogo
            {
                PosseJogo = new PosseJogo
                {
                    Jogo = new Jogo { Titulo = "Histórico preservado" },
                    Canal = new Canal { Nome = "PC" }
                },
                FinalizadoEm = DateTime.UtcNow,
                TempoEmHoras = 15
            };
            contexto.RegistrosJogos.Add(registro);
            await contexto.SaveChangesAsync();
            id = registro.Id;
        }
        var legado = await client.GetFromJsonAsync<JsonElement>($"/api/registros/{id}");
        Assert.Equal("Histórico preservado", legado.GetProperty("jogoTitulo").GetString());
        Assert.Equal(15, legado.GetProperty("tempoEmHoras").GetDecimal());
        Assert.Equal(HttpStatusCode.Gone, (await client.DeleteAsync($"/api/registros/{id}")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync($"/api/registros/{id}")).StatusCode);
        var inicio = await client.GetFromJsonAsync<DashboardInicioResponse>("/api/dashboard/inicio");
        Assert.Equal(0, inicio!.Resumo.JogosZerados);
        Assert.Equal(0, inicio.Resumo.HorasZeradas);
    }

    [Theory]
    [InlineData("POST", "/api/registros")]
    [InlineData("PUT", "/api/registrosjogos/1")]
    [InlineData("DELETE", "/api/registros/1")]
    public async Task RegistroLegadoTemApenasLeitura(string metodo, string rota)
    {
        using var client = factory.CreateClient();
        using var request = new HttpRequestMessage(new HttpMethod(metodo), rota)
        {
            Content = JsonContent.Create(new { posseJogoId = 1 })
        };
        using var resposta = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Gone, resposta.StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/registros")).StatusCode);
    }
}
