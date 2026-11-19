using System.Net;
using System.Net.Http.Json;
using gamelife_api.Contracts;

namespace gamelife_api.Tests.Api;

public sealed class MinhasAssinaturasEndpointsTests(GameLifeApiFactory factory) : IClassFixture<GameLifeApiFactory>
{
    [Fact]
    public async Task ConfiguraAssinaturaEAtualizaOJogoResgatadoPelaApi()
    {
        using var client = factory.CreateClient();
        const string url = "/api/minhas-assinaturas";
        var iniciais = await client.GetFromJsonAsync<MinhaAssinaturaResponse[]>(url);
        Assert.Equal(["Apple Arcade", "Game Pass", "Play Pass", "PlayStation Plus"], iniciais!.Select(item => item.Servico).ToArray());
        Assert.All(iniciais!, item => Assert.False(item.Ativa));

        var titulo = $"Resgate mensal {Guid.NewGuid()}";
        var criado = await client.PostAsJsonAsync("/biblioteca", new
        {
            titulo,
            plataformas = new[] { new { plataforma = "PSN", resgatadoDeAssinatura = true, servicoExigido = "ps plus" } }
        });
        Assert.Equal(HttpStatusCode.Created, criado.StatusCode);
        var jogo = (await criado.Content.ReadFromJsonAsync<JogoBibliotecaResponse>())!;
        Assert.Equal("BloqueadoSemAssinatura", Assert.Single(jogo.Plataformas).EstadoAcesso);

        var invalida = await client.PutAsJsonAsync(url, new { servico = "Netflix", ativa = true });
        Assert.Equal(HttpStatusCode.BadRequest, invalida.StatusCode);
        var ativada = await client.PutAsJsonAsync(url, new { servico = "ps plus", ativa = true, proximaRenovacao = "2026-12-13" });
        ativada.EnsureSuccessStatusCode();
        var assinatura = (await ativada.Content.ReadFromJsonAsync<MinhaAssinaturaResponse>())!;
        Assert.Equal("PlayStation Plus", assinatura.Servico);
        Assert.Equal(new DateTime(2026, 12, 13), assinatura.ProximaRenovacao);
        Assert.Equal("JogavelPorResgate", await EstadoAtualAsync());

        var desativada = await client.PutAsJsonAsync(url, new { servico = "PlayStation Plus", ativa = false });
        desativada.EnsureSuccessStatusCode();
        Assert.Equal("BloqueadoSemAssinatura", await EstadoAtualAsync());

        async Task<string> EstadoAtualAsync()
        {
            var jogos = await client.GetFromJsonAsync<JogoBibliotecaResponse[]>("/biblioteca");
            return Assert.Single(jogos!.Single(item => item.JogoId == jogo.JogoId).Plataformas).EstadoAcesso;
        }
    }
}
