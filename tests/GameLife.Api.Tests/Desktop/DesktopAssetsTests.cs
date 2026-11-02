using System.Windows.Media.Imaging;

namespace gamelife_api.Tests.Desktop;

public sealed class DesktopAssetsTests
{
    [Fact]
    public void IconePodeSerDecodificadoPeloWpf()
    {
        var caminho = Path.Combine(AppContext.BaseDirectory, "Assets", "GameLife.ico");

        var frame = BitmapFrame.Create(new Uri(caminho));

        Assert.True(frame.PixelWidth > 0);
        Assert.True(frame.PixelHeight > 0);
    }
}
