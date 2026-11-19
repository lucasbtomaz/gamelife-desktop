using System.ComponentModel.DataAnnotations;

namespace gamelife_api.Contracts;

public sealed record AtualizarPerfilRequest(
    [Required, MaxLength(100)] string Nome,
    string? FotoDataUrl,
    bool ExibirJogosRecentes);

public sealed record PerfilResponse(int Id, string Nome, string? FotoDataUrl, bool ExibirJogosRecentes, DateTime DataEntrada);
