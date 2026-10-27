using System.Net;
using System.Net.Http.Json;
using gamelife_api.Context;
using gamelife_api.Controllers;
using Microsoft.EntityFrameworkCore;

namespace gamelife_api.Tests.Api;

public sealed class BibliotecaEndpointsTests : IClassFixture<GameLifeApiFactory>
{
    private readonly HttpClient _client;

    public BibliotecaEndpointsTests(GameLifeApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task CadastrarEConsultarJogoPreservaPlataformaEMidiaFisica()
    {
        var cadastro = new
        {
            titulo = "Hades",
            plataforma = "PC",
            midiaFisica = true
        };

        var respostaCadastro = await _client.PostAsJsonAsync("/biblioteca", cadastro);

        Assert.Equal(HttpStatusCode.Created, respostaCadastro.StatusCode);
        var criado = await respostaCadastro.Content.ReadFromJsonAsync<ItemBibliotecaResponse>();
        Assert.NotNull(criado);
        Assert.Equal("Hades", criado.Titulo);
        Assert.Equal("PC", criado.Plataforma);
        Assert.True(criado.MidiaFisica);

        var itens = await _client.GetFromJsonAsync<List<ItemBibliotecaResponse>>("/biblioteca");
        var persistido = Assert.Single(itens!);
        Assert.Equal(criado.Id, persistido.Id);
        Assert.True(persistido.MidiaFisica);
    }

    [Fact]
    public void ConsultaDaBibliotecaEhTraduzivelPeloMySql()
    {
        var options = new DbContextOptionsBuilder<GameLifeDbContext>()
            .UseMySQL("Server=localhost;Database=teste;User=teste;Password=teste")
            .Options;
        using var contexto = new GameLifeDbContext(options);

        var sql = BibliotecaController.CriarConsulta(contexto).ToQueryString();

        Assert.Contains("ORDER BY", sql, StringComparison.OrdinalIgnoreCase);
    }

    private sealed record ItemBibliotecaResponse(int Id, string Titulo, string Plataforma, bool MidiaFisica);
}
