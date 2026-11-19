using System.ComponentModel.DataAnnotations;

namespace gamelife_api.Models;

public sealed class MinhaAssinatura
{
    public int Id { get; set; }

    [Required, MaxLength(60)]
    public string Servico { get; set; } = string.Empty;

    public bool Ativa { get; set; }
    public DateTime? ProximaRenovacao { get; set; }
}
