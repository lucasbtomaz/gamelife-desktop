using System.Net.Http.Json;
using gamelife_api.Context;
using gamelife_api.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;

namespace gamelife_api.Tests.Api;

public sealed class DashboardResumoEndpointsTests : IClassFixture<GameLifeApiFactory>
{
    private readonly HttpClient _client;
    private readonly GameLifeApiFactory _factory;

    public DashboardResumoEndpointsTests(GameLifeApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task RefleteBibliotecaFinalizacoesHorasConquistasPlataformasEAnos()
    {
        var steam = await AdicionarJogo("Hades", "STEAM");
        var playstation = await AdicionarJogo("Astro Bot", "PlayStation 5");

        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var contexto = scope.ServiceProvider.GetRequiredService<GameLifeDbContext>();
            contexto.AcompanhamentosJogos.AddRange(
                new AcompanhamentoJogo
                {
                    JogoId = steam.JogoId,
                    CanalId = await contexto.PossesJogos.Where(item => item.Id == steam.Id).Select(item => item.CanalId).SingleAsync(),
                    PosseJogoId = steam.Id,
                    Status = StatusAcompanhamentoJogo.Zerado,
                    DataStatus = new DateTime(2024, 6, 15, 20, 30, 0, DateTimeKind.Utc),
                    TempoEmHoras = 31.5m,
                    ConquistasPossiveis = 100,
                    ConquistasObtidas = 69
                },
                new AcompanhamentoJogo
                {
                    JogoId = playstation.JogoId,
                    CanalId = await contexto.PossesJogos.Where(item => item.Id == playstation.Id).Select(item => item.CanalId).SingleAsync(),
                    PosseJogoId = playstation.Id,
                    Status = StatusAcompanhamentoJogo.NaoTerminado,
                    TempoEmHoras = 120m
                });
            await contexto.SaveChangesAsync();
        }

        var dashboard = await _client.GetFromJsonAsync<DashboardResponse>("/api/dashboard/inicio");

        Assert.NotNull(dashboard);
        Assert.Equal(new DateTime(2020, 9, 29), dashboard.Perfil.DataEntrada.Date);
        Assert.Equal(2, dashboard.Resumo.TotalJogos);
        Assert.Equal(1, dashboard.Resumo.JogosZerados);
        Assert.Equal(31.5m, dashboard.Resumo.HorasZeradas);
        Assert.Equal(100, dashboard.Resumo.ConquistasPossiveis);
        Assert.Equal(69, dashboard.Resumo.ConquistasObtidas);
        Assert.Equal(69.00m, dashboard.Resumo.PercentualConquistas);
        Assert.Equal(1, TotalDa(dashboard.BibliotecaPorPlataforma, "STEAM"));
        Assert.Equal(1, TotalDa(dashboard.BibliotecaPorPlataforma, "PSN"));
        Assert.Equal(0, TotalDa(dashboard.BibliotecaPorPlataforma, "AMAZON PRIME"));
        Assert.Equal(1, TotalDa(dashboard.ZeradosPorPlataforma, "STEAM"));
        Assert.Equal(0, TotalDa(dashboard.ZeradosPorPlataforma, "PSN"));
        Assert.Equal(1, dashboard.ZeradosPorAno.Single(item => item.Ano == 2024).Total);
        Assert.Equal(0, dashboard.ZeradosPorAno.Single(item => item.Ano == 2043).Total);
    }

    private async Task<ItemBibliotecaResponse> AdicionarJogo(string titulo, string plataforma)
    {
        var response = await _client.PostAsJsonAsync("/biblioteca", new
        {
            titulo,
            plataforma,
            midiaFisica = false
        });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<ItemBibliotecaResponse>())!;
    }

    private static int TotalDa(IEnumerable<DistribuicaoResponse> itens, string nome)
        => itens.Single(item => item.Plataforma == nome).Total;

    private sealed record ItemBibliotecaResponse(int Id, int JogoId, string Titulo, string Plataforma, bool MidiaFisica);
    private sealed record PerfilResponse(DateTime DataEntrada);
    private sealed record ResumoResponse(
        int TotalJogos,
        int JogosZerados,
        decimal HorasZeradas,
        int ConquistasPossiveis,
        int ConquistasObtidas,
        decimal PercentualConquistas);
    private sealed record DistribuicaoResponse(string Plataforma, int Total);
    private sealed record ZeradosAnoResponse(int Ano, int Total);
    private sealed record DashboardResponse(
        PerfilResponse Perfil,
        ResumoResponse Resumo,
        DistribuicaoResponse[] BibliotecaPorPlataforma,
        DistribuicaoResponse[] ZeradosPorPlataforma,
        ZeradosAnoResponse[] ZeradosPorAno);
}
