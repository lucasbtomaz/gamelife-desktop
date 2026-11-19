using System.ComponentModel.DataAnnotations;

namespace gamelife_api.Contracts;

public sealed record SalvarCanalRequest(
    [Required, MaxLength(100)] string Nome, bool PermitePosse, bool PermitePreco);

public sealed record CanalResponse(int Id, string Nome, bool PermitePosse, bool PermitePreco);
public sealed record CanalResumoResponse(int Id, string Nome, bool PermitePosse, bool PermitePreco, int PossesCount);
public sealed record CanalIdentificacaoResponse(int Id, string Nome);

public sealed record CriarJogoRequest(
    [Required, MaxLength(200)] string Titulo, [Range(1, 9999)] int? AnoLancamento);

public sealed record AtualizarJogoRequest(
    [Required, MaxLength(200)] string Titulo, [Range(1, 9999)] int? AnoLancamento, int Id = 0);

public sealed record JogoResponse(int Id, string Titulo, int? AnoLancamento);
public sealed record JogoResumoResponse(int Id, string Titulo, int? AnoLancamento, int PossesCount, bool NaListaDesejos);
public sealed record JogoDetalheResponse(
    int Id, string Titulo, int? AnoLancamento, IEnumerable<JogoPosseResponse> Posses, JogoDesejoResponse? ItemListaDesejos);
public sealed record JogoPosseResponse(int Id, int CanalId, string? CanalNome, DateTime? AdquiridoEm);
public sealed record JogoDesejoResponse(
    int Id, decimal? PrecoDesejado, string? Observacao, DateTime CriadoEm,
    IEnumerable<JogoMotivoResponse> Motivos, IEnumerable<JogoPrecoResponse> Precos);
public sealed record JogoMotivoResponse(int Id, string Descricao);
public sealed record JogoPrecoResponse(int Id, int CanalId, decimal PrecoCheio, decimal PrecoAtual, bool Disponivel);

public sealed record CriarPosseRequest(
    [Range(1, int.MaxValue)] int JogoId,
    [Range(1, int.MaxValue)] int CanalId,
    bool CompraDigital, bool MidiaFisica, bool PorAssinatura,
    bool ResgatadoDeAssinatura, [MaxLength(60)] string? ServicoExigido);

public sealed record PosseResumoResponse(
    int Id, int JogoId, int CanalId, DateTime? AdquiridoEm, JogoResponse? Jogo, CanalIdentificacaoResponse? Canal);
public sealed record PosseCriadaResponse(
    int Id, int JogoId, int CanalId, DateTime? AdquiridoEm, bool CompraDigital, bool MidiaFisica,
    bool PorAssinatura, bool ResgatadoDeAssinatura, string? ServicoExigido);

public sealed record AtualizarPrecoRequest(
    [Range(typeof(decimal), "0", "79228162514264337593543950335")] decimal PrecoCheio,
    [Range(typeof(decimal), "0", "79228162514264337593543950335")] decimal PrecoAtual,
    bool Disponivel);

public sealed record CriarPrecoRequest(
    [Range(1, int.MaxValue)] int ItemListaDesejosId,
    [Range(1, int.MaxValue)] int CanalId,
    [Range(typeof(decimal), "0", "79228162514264337593543950335")] decimal PrecoCheio,
    [Range(typeof(decimal), "0", "79228162514264337593543950335")] decimal PrecoAtual,
    bool Disponivel);

public sealed record PrecoResponse(
    int Id, int ItemListaDesejosId, int CanalId, decimal PrecoCheio, decimal PrecoAtual,
    bool Disponivel, DateTime ConsultadoEm);
public sealed record PrecoResumoResponse(
    int Id, int ItemListaDesejosId, int CanalId, decimal PrecoCheio, decimal PrecoAtual,
    bool Disponivel, DateTime ConsultadoEm, string? CanalNome, string? JogoTitulo);

public sealed record CriarMotivoRequest(
    [Range(1, int.MaxValue)] int ItemListaDesejosId, [Required, MaxLength(500)] string Descricao);
public sealed record MotivoResponse(int Id, int ItemListaDesejosId, string Descricao);
