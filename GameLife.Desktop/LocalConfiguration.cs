using System.IO;
using System.Text.Json;

namespace GameLife.Desktop;

public static class LocalConfiguration
{
    public static string CaminhoPadrao => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "GameLife",
        "appsettings.Local.json");

    public static string ObterConnectionString(
        string? caminho = null,
        string? caminhoAlternativo = null)
    {
        var caminhoPrincipal = caminho ?? CaminhoPadrao;
        if (caminho is null && caminhoAlternativo is null)
            caminhoAlternativo = Path.Combine(AppContext.BaseDirectory, "appsettings.Local.json");

        foreach (var candidato in new[] { caminhoPrincipal, caminhoAlternativo })
        {
            if (string.IsNullOrWhiteSpace(candidato) || !File.Exists(candidato))
                continue;

            using var documento = JsonDocument.Parse(File.ReadAllText(candidato));
            if (documento.RootElement.TryGetProperty("ConnectionStrings", out var conexoes) &&
                conexoes.TryGetProperty("GameLife", out var valor) &&
                !string.IsNullOrWhiteSpace(valor.GetString()))
            {
                return valor.GetString()!;
            }
        }

        throw new InvalidOperationException(
            "A configuração local do MySQL não foi encontrada ou está incompleta. Reinstale o GameLife.");
    }
}
