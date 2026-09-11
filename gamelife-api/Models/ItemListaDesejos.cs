using System.ComponentModel.DataAnnotations;

namespace gamelife_api.Models
{
    public class ItemListaDesejos
    {
        public int Id { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Informe um ID de jogo válido.")]
        public int JogoId { get; set; }

        [MaxLength(1000, ErrorMessage = "A observação deve ter até 1000 caracteres.")]
        public string? Observacao { get; set; }

        [Range(typeof(decimal), "0", "79228162514264337593543950335", ErrorMessage = "O preço desejado não pode ser negativo.")]
        public decimal? PrecoDesejado { get; set; }
        public DateTime CriadoEm { get; set; }

        public Jogo Jogo { get; set; } = null!;
        public List<MotivoDesejo> Motivos { get; set; } = [];

        public ICollection<RegistroJogo> Registros { get; set; } = [];
    }
}
