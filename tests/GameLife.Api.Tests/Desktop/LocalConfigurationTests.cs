using GameLife.Desktop;

namespace gamelife_api.Tests.Desktop;

public sealed class LocalConfigurationTests : IDisposable
{
    private readonly string _diretorio = Path.Combine(Path.GetTempPath(), $"GameLifeTests-{Guid.NewGuid()}");

    [Fact]
    public void ObterConnectionStringPreservaValorDoArquivo()
    {
        Directory.CreateDirectory(_diretorio);
        var caminho = Path.Combine(_diretorio, "appsettings.Local.json");
        const string connectionString = "Server=localhost;Database=gamelife;User=usuario;Password=segredo com espaço";
        File.WriteAllText(caminho, $$"""
        {
          "ConnectionStrings": {
            "GameLife": "{{connectionString}}"
          }
        }
        """);

        var resultado = LocalConfiguration.ObterConnectionString(caminho);

        Assert.Equal(connectionString, resultado);
    }

    [Fact]
    public void ObterConnectionStringRejeitaArquivoAusente()
    {
        var caminho = Path.Combine(_diretorio, "inexistente.json");

        var erro = Assert.Throws<InvalidOperationException>(() =>
            LocalConfiguration.ObterConnectionString(caminho));

        Assert.Contains("configuração local", erro.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void TentarObterConnectionStringPermitePrimeiraInstalacaoSemMySql()
    {
        var caminho = Path.Combine(_diretorio, "inexistente.json");

        var resultado = LocalConfiguration.TentarObterConnectionString(caminho);

        Assert.Null(resultado);
    }

    [Fact]
    public void ObterConnectionStringUsaCopiaInstaladaQuandoConfiguracaoPrincipalNaoExiste()
    {
        Directory.CreateDirectory(_diretorio);
        var caminhoPrincipal = Path.Combine(_diretorio, "principal-inexistente.json");
        var caminhoInstalado = Path.Combine(_diretorio, "appsettings.Local.json");
        const string connectionString = "Server=localhost;Database=gamelife;User=usuario;Password=segredo";
        File.WriteAllText(caminhoInstalado, $$"""
        {
          "ConnectionStrings": {
            "GameLife": "{{connectionString}}"
          }
        }
        """);

        var resultado = LocalConfiguration.ObterConnectionString(
            caminhoPrincipal,
            caminhoInstalado);

        Assert.Equal(connectionString, resultado);
    }

    [Fact]
    public void ObterConnectionStringUsaCopiaInstaladaQuandoConfiguracaoPrincipalEstaVazia()
    {
        Directory.CreateDirectory(_diretorio);
        var caminhoPrincipal = Path.Combine(_diretorio, "principal.json");
        var caminhoInstalado = Path.Combine(_diretorio, "appsettings.Local.json");
        File.WriteAllText(caminhoPrincipal, """{"ConnectionStrings":{"GameLife":""}}""");
        const string connectionString = "Server=localhost;Database=gamelife;User=usuario;Password=segredo";
        File.WriteAllText(
            caminhoInstalado,
            "{\"ConnectionStrings\":{\"GameLife\":\"" + connectionString + "\"}}");

        var resultado = LocalConfiguration.ObterConnectionString(
            caminhoPrincipal,
            caminhoInstalado);

        Assert.Equal(connectionString, resultado);
    }

    public void Dispose()
    {
        if (Directory.Exists(_diretorio))
            Directory.Delete(_diretorio, recursive: true);
    }
}
