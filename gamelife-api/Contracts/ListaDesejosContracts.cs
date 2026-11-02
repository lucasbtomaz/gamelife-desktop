using System.ComponentModel.DataAnnotations;

namespace gamelife_api.Contracts;

public sealed record CriarItemListaDesejosRequest([Required, MaxLength(200)] string Titulo, [Range(typeof(decimal), "0", "9999999999999999")] decimal? PrecoDesejado, [MaxLength(1000)] string? Observacao);
public sealed record AtualizarItemListaDesejosRequest([Range(typeof(decimal), "0", "9999999999999999")] decimal? PrecoDesejado, [MaxLength(1000)] string? Observacao);
public sealed record AdicionarPrecoDesejoRequest(int? CanalId, [MaxLength(100)] string? NomeCanal, [Range(typeof(decimal), "0", "9999999999999999")] decimal PrecoAtual, [Range(typeof(decimal), "0", "9999999999999999")] decimal? PrecoCheio, bool Disponivel = true);
public sealed record PrecoAtualDesejoResponse(int CanalId, string CanalNome, decimal PrecoAtual, decimal PrecoCheio, bool Disponivel, DateTime ConsultadoEm, decimal? MenorPrecoHistorico);
public sealed record HistoricoPrecoDesejoResponse(int Id, int CanalId, string CanalNome, decimal PrecoAtual, decimal PrecoCheio, bool Disponivel, DateTime ConsultadoEm);
public sealed record ItemListaDesejosResponse(int Id, int JogoId, string Titulo, bool JaTenho, decimal? PrecoDesejado, string? Observacao, DateTime CriadoEm, IReadOnlyList<PrecoAtualDesejoResponse> PrecosAtuais, decimal? MenorPrecoAtual, decimal? MenorPrecoHistorico, IReadOnlyList<HistoricoPrecoDesejoResponse> HistoricoPrecos);
