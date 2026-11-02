using System.ComponentModel.DataAnnotations;

namespace gamelife_api.Models;

public sealed class Conquista
{
    public int Id { get; set; }

    [Required, MaxLength(150)]
    public string Titulo { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Descricao { get; set; }

    [Required, MaxLength(30)]
    public string Tipo { get; set; } = "Conquista";

    public int? JogoId { get; set; }
    public Jogo? Jogo { get; set; }
    public DateTime DesbloqueadaEm { get; set; } = DateTime.UtcNow;
}
