using System.ComponentModel.DataAnnotations;

namespace gamelife_api.Models
{
    public class MotivoDesejo
    {
        public int Id { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Informe um ID de item da lista de desejos válido.")]
        public int ItemListaDesejosId { get; set; }

        [Required(ErrorMessage = "Informe a descrição do motivo.")]
        [MaxLength(500, ErrorMessage = "A descrição deve ter até 500 caracteres.")]
        public string Descricao { get; set; } = string.Empty;

        public ItemListaDesejos ItemListaDesejos { get; set; } = null!;
    }
}
