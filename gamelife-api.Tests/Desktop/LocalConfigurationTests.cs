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

    public void Dispose()
    {
        if (Directory.Exists(_diretorio))
            Directory.Delete(_diretorio, recursive: true);
    }
}
