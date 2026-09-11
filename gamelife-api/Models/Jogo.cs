using System.ComponentModel.DataAnnotations;

namespace gamelife_api.Models
{
    public class Jogo
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Informe o título do jogo.")]
        [MaxLength(200, ErrorMessage = "O título deve ter até 200 caracteres.")]
        public string Titulo { get; set; } = string.Empty;

        [Range(1, 9999, ErrorMessage = "Informe um ano entre 1 e 9999.")]
        public int? AnoLancamento { get; set; }

        public ICollection<PosseJogo> PossesJogos { get; set; } = [];
    }
}
