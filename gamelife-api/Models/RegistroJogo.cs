using System.ComponentModel.DataAnnotations;

namespace gamelife_api.Models
{
    public class RegistroJogo
    {
        public int Id { get; set; }
        [Range(1, int.MaxValue, ErrorMessage = "Informe um ID de posse de jogo válido.")]
        public int PosseJogoId { get; set; }
        public DateTime? FinalizadoEm { get; set; }

        [Range(typeof(decimal), "0", "79228162514264337593543950335", ErrorMessage = "O tempo em horas não pode ser negativo.")]
        public decimal? TempoEmHoras { get; set; }
        [MaxLength(50, ErrorMessage = "A dificuldade deve ter até 50 caracteres.")]
        public string? Dificuldade { get; set; }
        [Range(0, int.MaxValue,ErrorMessage = "A quantidade de conquistas possíveis não pode ser negativa.")]
        public int? ConquistasPossiveis { get; set; }
        [Range(0, int.MaxValue,ErrorMessage = "A quantidade de conquistas obtidas não pode ser negativa.")]
        public int? ConquistasObtidas { get; set; }

        public PosseJogo PosseJogo { get; set; } = null!;
    }
}
