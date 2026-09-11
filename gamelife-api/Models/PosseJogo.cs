using System.ComponentModel.DataAnnotations;

namespace gamelife_api.Models
{
    public class PosseJogo
    {
        public int Id { get; set; }
        [Range(1, int.MaxValue, ErrorMessage = "Informe um ID de jogo válido.")]
        public int JogoId { get; set; }
        [Range(1, int.MaxValue, ErrorMessage = "Informe um ID de canal válido.")]
        public int CanalId { get; set; }
        public DateTime? AdquiridoEm { get; set; }

        public Jogo Jogo { get; set; } = null!;
        public Canal Canal { get; set; } = null!;
    }
}
