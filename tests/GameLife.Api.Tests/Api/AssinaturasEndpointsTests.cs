using System.Net;
using System.Net.Http.Json;
using gamelife_api.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace gamelife_api.Tests.Api;

public sealed class AssinaturasEndpointsTests : IClassFixture<GameLifeApiFactory>
{
    private readonly GameLifeApiFactory _factory;
    private readonly HttpClient _client;

    public AssinaturasEndpointsTests(GameLifeApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task CompartilhaAssinaturaEntreListaDeDesejosEBibliotecaSemDuplicar()
    {
        var titulo = $"Assinatura Compartilhada {Guid.NewGuid()}";
        var criarDesejo = await _client.PostAsJsonAsync("/api/lista-desejos", new { titulo });
        Assert.Equal(HttpStatusCode.Created, criarDesejo.StatusCode);
        var desejo = await criarDesejo.Content.ReadFromJsonAsync<DesejoResumo>();

        var salvarAssinatura = await _client.PostAsJsonAsync("/api/assinaturas", new
        {
            jogoId = desejo!.JogoId,
            servico = "game pass",
            situacao = "Anunciado",
            dataDisponibilidade = "2026-12-01"
        });
        Assert.Equal(HttpStatusCode.Created, salvarAssinatura.StatusCode);

        // Atualiza mesmo jogo e mesmo serviço -> não duplica e mantém estado mais recente
        var atualizarAssinatura = await _client.PutAsJsonAsync("/api/assinaturas", new
        {
            jogoId = desejo.JogoId,
            servico = "Game Pass",
            situacao = "Disponivel",
            dataDisponibilidade = "2026-11-15",
            dataEncerramento = "2027-05-15"
        });
        Assert.Equal(HttpStatusCode.OK, atualizarAssinatura.StatusCode);
        var atualizada = await atualizarAssinatura.Content.ReadFromJsonAsync<AssinaturaResponse>();
        Assert.Equal("Game Pass", atualizada!.Servico);
        Assert.Equal("Disponível", atualizada.Situacao);

        // Adiciona segunda assinatura para o mesmo jogo
        var segunda = await _client.PostAsJsonAsync("/api/assinaturas", new
        {
            jogoId = desejo.JogoId,
            servico = "PlayStation Plus",
            situacao = "Anunciado",
            dataDisponibilidade = "2027-01-10"
        });
        Assert.Equal(HttpStatusCode.Created, segunda.StatusCode);

        // Ao adicionar o jogo na Biblioteca (ex: compra digital), as assinaturas já cadastradas aparecem
        var bibliotecaResp = await _client.PostAsJsonAsync("/biblioteca", new
        {
            titulo,
            plataformas = new[]
            {
                new { plataforma = "PC", compraDigital = true, midiaFisica = false, porAssinatura = false }
            }
        });
        Assert.Equal(HttpStatusCode.OK, bibliotecaResp.StatusCode);
        var jogoBiblioteca = await bibliotecaResp.Content.ReadFromJsonAsync<JogoBibliotecaResumo>();
        Assert.Equal(2, jogoBiblioteca!.Assinaturas.Count);
        Assert.Contains(jogoBiblioteca.Assinaturas, item => item.Servico == "Game Pass" && item.Situacao == "Disponível");
        Assert.Contains(jogoBiblioteca.Assinaturas, item => item.Servico == "PlayStation Plus" && item.Situacao == "Anunciado");

        using var scope = _factory.Services.CreateScope();
        var contexto = scope.ServiceProvider.GetRequiredService<GameLifeDbContext>();
        Assert.Equal(2, await contexto.AssinaturasJogos.CountAsync(item => item.JogoId == desejo.JogoId));
    }

    [Fact]
    public async Task ExcluiAssinaturaERejeitaDatasOuServicoInvalidos()
    {
        var titulo = $"Assinatura Remocao {Guid.NewGuid()}";
        var criarDesejo = await _client.PostAsJsonAsync("/api/lista-desejos", new { titulo });
        var desejo = await criarDesejo.Content.ReadFromJsonAsync<DesejoResumo>();

        var invalidoServico = await _client.PostAsJsonAsync("/api/assinaturas", new
        {
            jogoId = desejo!.JogoId,
            servico = "Servico Inexistente",
            situacao = "Disponível"
        });
        Assert.Equal(HttpStatusCode.BadRequest, invalidoServico.StatusCode);

        var invalidaData = await _client.PostAsJsonAsync("/api/assinaturas", new
        {
            jogoId = desejo.JogoId,
            servico = "Apple Arcade",
            situacao = "Encerrado",
            dataDisponibilidade = "2026-10-10",
            dataEncerramento = "2026-09-01"
        });
        Assert.Equal(HttpStatusCode.BadRequest, invalidaData.StatusCode);

        var criada = await _client.PostAsJsonAsync("/api/assinaturas", new
        {
            jogoId = desejo.JogoId,
            servico = "Play Pass",
            situacao = "Disponível"
        });
        Assert.Equal(HttpStatusCode.Created, criada.StatusCode);
        var payload = await criada.Content.ReadFromJsonAsync<AssinaturaResponse>();

        var exclusao = await _client.DeleteAsync($"/api/assinaturas/{payload!.Id}");
        Assert.Equal(HttpStatusCode.NoContent, exclusao.StatusCode);

        var restantes = await _client.GetFromJsonAsync<AssinaturaResponse[]>($"/api/assinaturas?jogoId={desejo.JogoId}");
        Assert.Empty(restantes!);
    }

    private sealed record DesejoResumo(int Id, int JogoId);
    private sealed record AssinaturaResponse(int Id, int JogoId, string Servico, string Situacao);
    private sealed record JogoBibliotecaResumo(int JogoId, List<AssinaturaResponse> Assinaturas);
}
