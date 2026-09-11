using System.ComponentModel.DataAnnotations;

namespace gamelife_api.Models
{
    public class RegistroPreco
    {
        public int Id { get; set; }
        [Range(1, int.MaxValue,ErrorMessage = "Informe um ID de item da lista de desejos válido.")]
        public int ItemListaDesejosId { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Informe um ID de canal válido.")]
        public int CanalId { get; set; }

        [Range(typeof(decimal), "0", "79228162514264337593543950335",ErrorMessage = "O preço cheio não pode ser negativo.")]
        public decimal PrecoCheio { get; set; }

        [Range(typeof(decimal), "0", "79228162514264337593543950335",ErrorMessage = "O preço atual não pode ser negativo.")]
        public decimal PrecoAtual { get; set; }
        public bool Disponivel { get; set; }
        public DateTime ConsultadoEm { get; set; }

        public ItemListaDesejos ItemListaDesejos { get; set; } = null!;
        public Canal Canal { get; set; } = null!;
    }
}
