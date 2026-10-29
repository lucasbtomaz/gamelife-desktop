using System.IO;
using System.Text.Json;

namespace GameLife.Desktop;

public static class LocalConfiguration
{
    public static string CaminhoPadrao => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "GameLife",
        "appsettings.Local.json");

    public static string ObterConnectionString(string? caminho = null)
    {
        caminho ??= CaminhoPadrao;
        if (!File.Exists(caminho))
            throw new InvalidOperationException(
                "A configuração local do MySQL não foi encontrada. Recrie o atalho do GameLife.");

        using var documento = JsonDocument.Parse(File.ReadAllText(caminho));
        if (!documento.RootElement.TryGetProperty("ConnectionStrings", out var conexoes) ||
            !conexoes.TryGetProperty("GameLife", out var valor) ||
            string.IsNullOrWhiteSpace(valor.GetString()))
        {
            throw new InvalidOperationException(
                "A configuração local do MySQL está incompleta. Recrie o atalho do GameLife.");
        }

        return valor.GetString()!;
    }
}
