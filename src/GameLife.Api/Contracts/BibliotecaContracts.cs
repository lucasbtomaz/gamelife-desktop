using System.ComponentModel.DataAnnotations;

namespace gamelife_api.Contracts;

public sealed record SalvarJogoBibliotecaRequest(
    [Required, MaxLength(200)] string Titulo,
    [Range(1, 9999)] int? AnoLancamento,
    [Required, MinLength(1)] IReadOnlyList<SalvarPlataformaBibliotecaRequest> Plataformas);

public sealed record SalvarPlataformaBibliotecaRequest(
    [Required, MaxLength(100)] string Plataforma,
    bool MidiaFisica,
    SalvarAcompanhamentoBibliotecaRequest? Acompanhamento);

public sealed record SalvarAcompanhamentoBibliotecaRequest(
    string? Status,
    DateTime? DataStatus,
    [Range(typeof(decimal), "0", "79228162514264337593543950335")] decimal? TempoEmHoras,
    [Range(0, int.MaxValue)] int? ConquistasPossiveis,
    [Range(0, int.MaxValue)] int? ConquistasObtidas);

public sealed record JogoBibliotecaResponse(
    int JogoId,
    string Titulo,
    int? AnoLancamento,
    IReadOnlyList<PlataformaBibliotecaResponse> Plataformas);

public sealed record PlataformaBibliotecaResponse(
    int PosseId,
    int CanalId,
    string Nome,
    bool MidiaFisica,
    AcompanhamentoBibliotecaResponse? Acompanhamento);

public sealed record AcompanhamentoBibliotecaResponse(
    int Id,
    string Status,
    DateTime? DataStatus,
    decimal? TempoEmHoras,
    int? ConquistasPossiveis,
    int? ConquistasObtidas);

public sealed record ItemBibliotecaPlataformaProjection(
    int PosseId,
    int JogoId,
    string Titulo,
    int? AnoLancamento,
    int CanalId,
    string Plataforma,
    bool MidiaFisica,
    int? AcompanhamentoId,
    string? Status,
    DateTime? DataStatus,
    decimal? TempoEmHoras,
    int? ConquistasPossiveis,
    int? ConquistasObtidas);
