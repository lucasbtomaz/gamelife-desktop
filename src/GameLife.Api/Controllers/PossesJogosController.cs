using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using gamelife_api.Context;
using gamelife_api.Models;

namespace gamelife_api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Route("api/posses")]
    public class PossesJogosController : ControllerBase
    {
        private readonly GameLifeDbContext _contexto;

        public PossesJogosController(GameLifeDbContext contexto)
        {
            _contexto = contexto;
        }

        // GET: api/posses-jogos
        [HttpGet]
        public async Task<ActionResult<IEnumerable<object>>> ObterPosses()
        {
            var posses = await _contexto.PossesJogos
                .Include(p => p.Jogo)
                .Include(p => p.Canal)
                .Select(p => new
                {
                    p.Id,
                    p.JogoId,
                    p.CanalId,
                    p.AdquiridoEm,
                    Jogo = p.Jogo != null ? new { p.Jogo.Id, p.Jogo.Titulo, p.Jogo.AnoLancamento } : null,
                    Canal = p.Canal != null ? new { p.Canal.Id, p.Canal.Nome } : null
                })
                .ToListAsync();

            return Ok(posses);
        }

        // GET: api/posses-jogos/5
        [HttpGet("{id}")]
        public async Task<ActionResult<object>> ObterPossePorId(int id)
        {
            var posse = await _contexto.PossesJogos
                .Include(p => p.Jogo)
                .Include(p => p.Canal)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (posse == null)
            {
                return NotFound(new { mensagem = $"Posse com ID {id} não encontrada." });
            }

            return Ok(new
            {
                posse.Id,
                posse.JogoId,
                posse.CanalId,
                posse.AdquiridoEm,
                Jogo = posse.Jogo != null ? new { posse.Jogo.Id, posse.Jogo.Titulo, posse.Jogo.AnoLancamento } : null,
                Canal = posse.Canal != null ? new { posse.Canal.Id, posse.Canal.Nome } : null
            });
        }

        // POST: api/posses-jogos
        [HttpPost]
        public async Task<ActionResult<PosseJogo>> CriarPosse([FromBody] PosseJogo posse)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var canal = await _contexto.Canais.FindAsync(posse.CanalId);
            if (canal == null)
            {
                return BadRequest(new { mensagem = $"Canal com ID {posse.CanalId} não encontrado." });
            }

            if (!canal.PermitePosse)
            {
                return BadRequest(new { mensagem = $"O canal '{canal.Nome}' não aceita posse de jogos." });
            }

            var jaPossui = await _contexto.PossesJogos.AnyAsync(p => p.JogoId == posse.JogoId && p.CanalId == posse.CanalId);
            if (jaPossui)
            {
                return BadRequest(new { mensagem = "Este jogo já está vinculado a este canal." });
            }

            if (posse.AdquiridoEm == null)
            {
                posse.AdquiridoEm = DateTime.UtcNow;
            }

            _contexto.PossesJogos.Add(posse);
            await _contexto.SaveChangesAsync();

            return CreatedAtAction(nameof(ObterPossePorId), new { id = posse.Id }, posse);
        }

        // DELETE: api/posses-jogos/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> ExcluirPosse(int id)
        {
            var posse = await _contexto.PossesJogos.FindAsync(id);
            if (posse == null)
            {
                return NotFound(new { mensagem = $"Posse com ID {id} não encontrada." });
            }

            _contexto.PossesJogos.Remove(posse);
            await _contexto.SaveChangesAsync();

            return NoContent();
        }
    }
}
