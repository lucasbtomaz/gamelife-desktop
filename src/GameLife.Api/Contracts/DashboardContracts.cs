namespace gamelife_api.Contracts;

public sealed record DashboardInicioResponse(
    PerfilResponse Perfil,
    DashboardResumoResponse Resumo,
    IReadOnlyList<DashboardDistribuicaoResponse> BibliotecaPorPlataforma,
    IReadOnlyList<DashboardDistribuicaoResponse> ZeradosPorPlataforma,
    IReadOnlyList<DashboardAnoResponse> ZeradosPorAno,
    IReadOnlyList<DashboardJogoRecenteResponse> JogosRecentes,
    IReadOnlyList<DashboardConquistaResponse> Conquistas,
    IReadOnlyList<DashboardAlertaDesejoResponse> AlertasListaDesejos,
    IReadOnlyList<DashboardSaidaCatalogoResponse> AlertasSaidaCatalogo);

public sealed record DashboardResumoResponse(
    int TotalJogos, int MidiasFisicas, int JogosDigitais, int TotalColecoes, int TempoNaPlataformaAnos,
    int JogosZerados, decimal HorasZeradas, int ConquistasPossiveis, int ConquistasObtidas,
    decimal PercentualConquistas);
public sealed record DashboardDistribuicaoResponse(string Plataforma, int Total);
public sealed record DashboardAnoResponse(int Ano, int Total);
public sealed record DashboardJogoRecenteResponse(
    int JogoId, string Titulo, string Plataforma, bool CompraDigital, bool MidiaFisica,
    bool PorAssinatura, DateTime? AdquiridoEm);
public sealed record DashboardConquistaResponse(
    int Id, string Titulo, string? Descricao, string Tipo, string? Jogo, DateTime DesbloqueadaEm);
public sealed record DashboardAlertaDesejoResponse(
    int DesejoId, int JogoId, string Titulo, string[] LojasMonitoradas, decimal? MenorPrecoAtual);
public sealed record DashboardSaidaCatalogoResponse(
    int JogoId, string Titulo, string Servico, DateTime DataEncerramento, int DiasRestantes);
