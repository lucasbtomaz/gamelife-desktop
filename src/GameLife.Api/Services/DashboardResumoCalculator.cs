using System.Globalization;
using System.Text;

namespace gamelife_api.Services;

public static class DashboardResumoCalculator
{
    public static IReadOnlyList<string> PlataformasPrincipais { get; } =
    [
        "AMAZON PRIME",
        "BLIZZARD",
        "EA",
        "EPIC",
        "GOG",
        "NINTENDO",
        "PSN",
        "STEAM",
        "UBISOFT",
        "XBOX",
        "PLAY STORE"
    ];

    public static int CalcularAnosCompletos(DateOnly inicio, DateOnly hoje)
    {
        if (hoje < inicio) return 0;

        var anos = hoje.Year - inicio.Year;
        if (hoje < inicio.AddYears(anos)) anos--;
        return anos;
    }

    public static string NormalizarPlataforma(string nome)
    {
        var nomeLimpo = nome.Trim();
        var comparacao = RemoverDiacriticos(nomeLimpo).ToUpperInvariant();

        if (comparacao.StartsWith("PLAYSTATION") || comparacao == "PSN") return "PSN";
        if (comparacao.StartsWith("XBOX")) return "XBOX";
        if (comparacao.StartsWith("NINTENDO")) return "NINTENDO";
        if (comparacao is "AMAZON PRIME" or "PRIME GAMING" or "AMAZON LUNA") return "AMAZON PRIME";
        if (comparacao is "BLIZZARD" or "BATTLE.NET" or "BATTLENET") return "BLIZZARD";
        if (comparacao is "EA" or "ELECTRONIC ARTS" or "EA APP") return "EA";
        if (comparacao.StartsWith("EPIC")) return "EPIC";
        if (comparacao == "GOG") return "GOG";
        if (comparacao == "STEAM") return "STEAM";
        if (comparacao.StartsWith("UBISOFT")) return "UBISOFT";
        if (comparacao is "PLAY STORE" or "GOOGLE PLAY") return "PLAY STORE";

        return nomeLimpo;
    }

    private static string RemoverDiacriticos(string valor)
    {
        var caracteres = valor.Normalize(NormalizationForm.FormD)
            .Where(caractere => CharUnicodeInfo.GetUnicodeCategory(caractere) != UnicodeCategory.NonSpacingMark)
            .ToArray();
        return new string(caracteres).Normalize(NormalizationForm.FormC);
    }
}
