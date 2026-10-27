using Microsoft.Extensions.Configuration;

namespace gamelife_api.Tests.Configuration;

public sealed class StartupConfigurationTests
{
    [Fact]
    public void InicializacaoSemConnectionStringFalhaComMensagemSegura()
    {
        var configuration = new ConfigurationBuilder().Build();

        var exception = Assert.Throws<InvalidOperationException>(
            () => GameLifeStartup.ObterConnectionString(configuration));

        Assert.Contains("ConnectionStrings:GameLife", exception.Message);
        Assert.DoesNotContain("Password=", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Pwd=", exception.Message, StringComparison.OrdinalIgnoreCase);
    }
}
