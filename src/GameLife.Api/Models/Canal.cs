using System.ComponentModel.DataAnnotations;

namespace gamelife_api.Models
{
    public class Canal
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Informe o nome do canal.")]
        [MaxLength(100, ErrorMessage = "O nome deve ter até 100 caracteres.")]
        public string Nome { get; set; } = string.Empty;
        public bool PermitePosse { get; set; }
        public bool PermitePreco { get; set; }

        public ICollection<PosseJogo> PossesJogos { get; set; } = [];
        public ICollection<RegistroPreco> RegistrosPrecos { get; set; } = [];

    }
}
