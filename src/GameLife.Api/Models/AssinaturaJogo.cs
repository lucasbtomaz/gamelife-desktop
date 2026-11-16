using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Text;

namespace gamelife_api.Models;

public sealed class AssinaturaJogo
{
    public int Id { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Informe um ID de jogo válido.")]
    public int JogoId { get; set; }

    [Required(ErrorMessage = "Informe o serviço da assinatura.")]
    [MaxLength(60, ErrorMessage = "O serviço deve ter até 60 caracteres.")]
    public string Servico { get; set; } = string.Empty;

    [Required(ErrorMessage = "Informe a situação da assinatura.")]
    [MaxLength(30, ErrorMessage = "A situação deve ter até 30 caracteres.")]
    public string Situacao { get; set; } = string.Empty;

    public DateTime? DataDisponibilidade { get; set; }

    public DateTime? DataEncerramento { get; set; }

    public Jogo Jogo { get; set; } = null!;
}

public static class AssinaturaCatalogo
{
    public static readonly IReadOnlyList<string> ServicosSuportados =
    [
        "Game Pass",
        "PlayStation Plus",
        "Apple Arcade",
        "Play Pass"
    ];

    public static readonly IReadOnlyList<string> SituacoesSuportadas =
    [
        "Anunciado",
        "Disponível",
        "Encerrado"
    ];

    public static string NormalizarServico(string? valor)
    {
        if (string.IsNullOrWhiteSpace(valor))
            throw new ArgumentException("Informe o serviço da assinatura.");

        var chave = NormalizarComparacao(valor);
        return chave switch
        {
            "GAME PASS" or "GAMEPASS" or "XBOX GAME PASS" or "PC GAME PASS" => "Game Pass",
            "PLAYSTATION PLUS" or "PS PLUS" or "PSPLUS" => "PlayStation Plus",
            "APPLE ARCADE" or "APPLEARCADE" => "Apple Arcade",
            "PLAY PASS" or "PLAYPASS" or "GOOGLE PLAY PASS" => "Play Pass",
            _ => throw new ArgumentException("Serviço de assinatura inválido. Use Game Pass, PlayStation Plus, Apple Arcade ou Play Pass.")
        };
    }

    public static string NormalizarSituacao(string? valor)
    {
        if (string.IsNullOrWhiteSpace(valor))
            throw new ArgumentException("Informe a situação da assinatura.");

        var chave = NormalizarComparacao(valor);
        return chave switch
        {
            "ANUNCIADO" => "Anunciado",
            "DISPONIVEL" => "Disponível",
            "ENCERRADO" => "Encerrado",
            _ => throw new ArgumentException("Situação da assinatura inválida. Use Anunciado, Disponível ou Encerrado.")
        };
    }

    private static string NormalizarComparacao(string valor)
    {
        var semAcentos = new string(valor.Trim().Normalize(NormalizationForm.FormD)
            .Where(ch => CharUnicodeInfo.GetUnicodeCategory(ch) != UnicodeCategory.NonSpacingMark)
            .ToArray())
            .Normalize(NormalizationForm.FormKC)
            .ToUpperInvariant();
        return string.Join(' ', semAcentos.Split([' '], StringSplitOptions.RemoveEmptyEntries));
    }
}
