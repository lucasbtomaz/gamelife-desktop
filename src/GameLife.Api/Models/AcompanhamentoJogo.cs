using System.ComponentModel.DataAnnotations;

namespace gamelife_api.Models;

public enum StatusAcompanhamentoJogo
{
    NaoTerminado,
    Zerado,
    Dropado
}

public sealed class AcompanhamentoJogo
{
    public int Id { get; set; }

    [Range(1, int.MaxValue)]
    public int JogoId { get; set; }

    [Range(1, int.MaxValue)]
    public int CanalId { get; set; }

    public int? PosseJogoId { get; set; }
    public StatusAcompanhamentoJogo Status { get; set; }
    public DateTime? DataStatus { get; set; }

    [Range(typeof(decimal), "0", "79228162514264337593543950335")]
    public decimal? TempoEmHoras { get; set; }

    [Range(0, int.MaxValue)]
    public int? ConquistasPossiveis { get; set; }

    [Range(0, int.MaxValue)]
    public int? ConquistasObtidas { get; set; }

    public Jogo Jogo { get; set; } = null!;
    public Canal Canal { get; set; } = null!;
    public PosseJogo? PosseJogo { get; set; }
}
