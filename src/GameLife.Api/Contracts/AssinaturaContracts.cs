using System.ComponentModel.DataAnnotations;

namespace gamelife_api.Contracts;

public sealed record SalvarAssinaturaJogoRequest(
    int? JogoId,
    [MaxLength(200)] string? Titulo,
    [Required, MaxLength(60)] string Servico,
    [Required, MaxLength(30)] string Situacao,
    DateTime? DataDisponibilidade = null,
    DateTime? DataEncerramento = null);

public sealed record AssinaturaJogoResponse(
    int Id,
    int JogoId,
    string Servico,
    string Situacao,
    DateTime? DataDisponibilidade,
    DateTime? DataEncerramento);
