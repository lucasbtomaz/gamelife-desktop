using Microsoft.Extensions.Configuration;

namespace gamelife_api.Tests.Configuration;

public sealed class StartupConfigurationTests
{
    [Fact]
    public void UsaSqliteSomenteQuandoConfiguradoExplicitamente()
    {
        var sqlite = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["DatabaseProvider"] = "Sqlite"
            })
            .Build();
        var defaultConfiguration = new ConfigurationBuilder().Build();

        Assert.Equal(GameLifeStartup.ProvedorSqlite, GameLifeStartup.ObterProvedor(sqlite));
        Assert.Equal(GameLifeStartup.ProvedorMySql, GameLifeStartup.ObterProvedor(defaultConfiguration));
    }

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

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void InicializacaoComConnectionStringVaziaFalhaComMensagemSegura(string connectionString)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:GameLife"] = connectionString
            })
            .Build();

        var exception = Assert.Throws<InvalidOperationException>(
            () => GameLifeStartup.ObterConnectionString(configuration));

        Assert.Contains("ConnectionStrings:GameLife", exception.Message);
    }
}
