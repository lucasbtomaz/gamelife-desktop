using System.IO;

namespace GameLife.Desktop;

public static class DesktopAssets
{
    public static void Validar(string pasta)
    {
        if (!File.Exists(Path.Combine(pasta, "index.html")))
            throw new InvalidOperationException(
                "A interface do GameLife não foi encontrada. Publique o aplicativo novamente.");
    }
}
