using System.Text;

namespace gamelife_api.Services;

public static class ChaveTexto
{
    public static string NormalizarChave(string valor) =>
        valor.Trim().Normalize(NormalizationForm.FormKC).ToUpperInvariant();
}
