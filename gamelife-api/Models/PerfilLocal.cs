using System.ComponentModel.DataAnnotations;

namespace gamelife_api.Models;

public sealed class PerfilLocal
{
    public int Id { get; set; } = 1;

    [Required, MaxLength(100)]
    public string Nome { get; set; } = "Jogador";

    public string? FotoDataUrl { get; set; }

    public bool ExibirJogosRecentes { get; set; } = true;
}
