using System.ComponentModel.DataAnnotations;

namespace gamelife_api.Contracts;

public sealed record SalvarAcompanhamentoJogoRequest(
    [Required, MaxLength(200)] string Titulo,
    [Required, MaxLength(100)] string Plataforma,
    [Required] string Status,
    DateTime? DataStatus,
    [Range(typeof(decimal), "0", "79228162514264337593543950335")] decimal? TempoEmHoras,
    [Range(0, int.MaxValue)] int? ConquistasPossiveis,
    [Range(0, int.MaxValue)] int? ConquistasObtidas,
    bool AdicionarBiblioteca);

public sealed record AcompanhamentoJogoResponse(
    int Id,
    int JogoId,
    string Titulo,
    int CanalId,
    string Plataforma,
    string Status,
    DateTime? DataStatus,
    decimal? TempoEmHoras,
    int? ConquistasPossiveis,
    int? ConquistasObtidas,
    decimal PercentualConquistas,
    bool EstaNaBiblioteca);

public sealed record ResumoAcompanhamentosResponse(
    int Total,
    int Zerados,
    int Dropados,
    int NaoTerminados);
