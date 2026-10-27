using System.ComponentModel.DataAnnotations;

namespace gamelife_api.Models;

public sealed class Colecao
{
    public int Id { get; set; }

    [Required, MaxLength(100)]
    public string Nome { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Descricao { get; set; }

    public DateTime CriadaEm { get; set; } = DateTime.UtcNow;

    public ICollection<ColecaoJogo> Jogos { get; set; } = [];
}

public sealed class ColecaoJogo
{
    public int ColecaoId { get; set; }
    public Colecao Colecao { get; set; } = null!;
    public int JogoId { get; set; }
    public Jogo Jogo { get; set; } = null!;
    public DateTime AdicionadoEm { get; set; } = DateTime.UtcNow;
}
