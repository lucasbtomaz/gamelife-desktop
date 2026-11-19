using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using gamelife_api.Context;
using gamelife_api.Contracts;
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
        public async Task<ActionResult<IEnumerable<MotivoResponse>>> ObterMotivos()
        {
            return await _contexto.MotivosDesejos
                .Select(item => new MotivoResponse(item.Id, item.ItemListaDesejosId, item.Descricao))
                .ToListAsync();
        }

        // POST: api/motivos-desejos
        [HttpPost]
        public async Task<ActionResult<MotivoResponse>> CriarMotivo([FromBody] CriarMotivoRequest dados)
        {
            var itemExiste = await _contexto.ItensListaDesejos.AnyAsync(i => i.Id == dados.ItemListaDesejosId);
            if (!itemExiste)
            {
                return BadRequest(new { mensagem = $"Item da lista de desejos #{dados.ItemListaDesejosId} não encontrado." });
            }

            var motivo = new MotivoDesejo { ItemListaDesejosId = dados.ItemListaDesejosId, Descricao = dados.Descricao };
            _contexto.MotivosDesejos.Add(motivo);
            await _contexto.SaveChangesAsync();

            return CreatedAtAction(nameof(ObterMotivos), new { id = motivo.Id }, new MotivoResponse(motivo.Id, motivo.ItemListaDesejosId, motivo.Descricao));
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
