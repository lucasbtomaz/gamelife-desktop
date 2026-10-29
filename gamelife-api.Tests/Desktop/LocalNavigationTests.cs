using GameLife.Desktop;

namespace gamelife_api.Tests.Desktop;

public sealed class LocalNavigationTests
{
    [Theory]
    [InlineData("http://127.0.0.1:51234/", true)]
    [InlineData("http://127.0.0.1:51234/biblioteca", true)]
    [InlineData("http://127.0.0.1:5000/", false)]
    [InlineData("https://example.com/", false)]
    [InlineData("javascript:alert(1)", false)]
    public void PermiteSomenteOrigemLocalExata(string destino, bool esperado)
    {
        var origem = new Uri("http://127.0.0.1:51234/");

        Assert.Equal(esperado, LocalNavigation.EhPermitida(origem, destino));
    }
}
