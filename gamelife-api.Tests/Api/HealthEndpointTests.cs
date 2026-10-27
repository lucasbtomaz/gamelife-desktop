using System.Net;
using System.Net.Http.Json;

namespace gamelife_api.Tests.Api;

public sealed class HealthEndpointTests : IClassFixture<GameLifeApiFactory>
{
    private readonly HttpClient _client;

    public HealthEndpointTests(GameLifeApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task IdentificaGameLifeQuandoAplicacaoEBancoEstaoProntos()
    {
        var response = await _client.GetAsync("/health");
        var health = await response.Content.ReadFromJsonAsync<HealthResponse>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(health);
        Assert.Equal("GameLife", health.Application);
        Assert.Equal("ready", health.Status);
    }

    private sealed record HealthResponse(string Application, string Status);
}
