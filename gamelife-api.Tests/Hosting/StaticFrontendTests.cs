using gamelife_api.Tests.Api;

namespace gamelife_api.Tests.Hosting;

public sealed class StaticFrontendTests : IClassFixture<GameLifeApiFactory>
{
    private readonly HttpClient _client;

    public StaticFrontendTests(GameLifeApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Theory]
    [InlineData("/")]
    [InlineData("/biblioteca-app")]
    public async Task ServeAplicacaoReactEmRotasDoNavegador(string rota)
    {
        var response = await _client.GetAsync(rota);
        var content = await response.Content.ReadAsStringAsync();

        response.EnsureSuccessStatusCode();
        Assert.Equal("text/html", response.Content.Headers.ContentType?.MediaType);
        Assert.Contains("<div id=\"root\"></div>", content);
    }
}
