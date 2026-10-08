using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using gamelife_api.Context;
using gamelife_api.Models;

namespace gamelife_api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Route("api/motivos")]
    public class MotivosDesejosController : ControllerBase
    {
        private readonly GameLifeDbContext _contexto;

        public MotivosDesejosController(GameLifeDbContext contexto)
        {
            _contexto = contexto;
        }

        // GET: api/motivos-desejos
        [HttpGet]
        public async Task<ActionResult<IEnumerable<MotivoDesejo>>> ObterMotivos()
        {
            return await _contexto.MotivosDesejos.ToListAsync();
        }

        // POST: api/motivos-desejos
        [HttpPost]
        public async Task<ActionResult<MotivoDesejo>> CriarMotivo([FromBody] MotivoDesejo motivo)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var itemExiste = await _contexto.ItensListaDesejos.AnyAsync(i => i.Id == motivo.ItemListaDesejosId);
            if (!itemExiste)
            {
                return BadRequest(new { mensagem = $"Item da lista de desejos #{motivo.ItemListaDesejosId} não encontrado." });
            }

            _contexto.MotivosDesejos.Add(motivo);
            await _contexto.SaveChangesAsync();

            return CreatedAtAction(nameof(ObterMotivos), new { id = motivo.Id }, motivo);
        }

        // DELETE: api/motivos-desejos/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> ExcluirMotivo(int id)
        {
            var motivo = await _contexto.MotivosDesejos.FindAsync(id);
            if (motivo == null)
            {
                return NotFound(new { mensagem = $"Motivo com ID {id} não encontrado." });
            }

            _contexto.MotivosDesejos.Remove(motivo);
            await _contexto.SaveChangesAsync();

            return NoContent();
        }
    }
}
