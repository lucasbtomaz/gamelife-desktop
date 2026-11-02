using GameLife.Desktop;

namespace gamelife_api.Tests.Desktop;

public sealed class DesktopAssetValidationTests
{
    [Fact]
    public void ValidarRejeitaPublicacaoSemIndexHtml()
    {
        var pasta = Path.Combine(Path.GetTempPath(), $"GameLifeAssets-{Guid.NewGuid()}");
        Directory.CreateDirectory(pasta);
        try
        {
            var erro = Assert.Throws<InvalidOperationException>(() => DesktopAssets.Validar(pasta));
            Assert.Contains("interface", erro.Message, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            Directory.Delete(pasta, recursive: true);
        }
    }
}
