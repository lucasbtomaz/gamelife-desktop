using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using gamelife_api.Context;
using gamelife_api.Models;

namespace gamelife_api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CanaisController : ControllerBase
    {
        private readonly GameLifeDbContext _contexto;

        public CanaisController(GameLifeDbContext contexto)
        {
            _contexto = contexto;
        }

        // GET: api/canais
        [HttpGet]
        public async Task<ActionResult<IEnumerable<object>>> ObterCanais()
        {
            var canais = await _contexto.Canais
                .Select(c => new
                {
                    c.Id,
                    c.Nome,
                    c.PermitePosse,
                    c.PermitePreco,
                    PossesCount = _contexto.PossesJogos.Count(p => p.CanalId == c.Id)
                })
                .ToListAsync();

            return Ok(canais);
        }

        // GET: api/canais/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Canal>> ObterCanalPorId(int id)
        {
            var canal = await _contexto.Canais.FindAsync(id);
            if (canal == null)
            {
                return NotFound(new { mensagem = $"Canal com ID {id} não encontrado." });
            }

            return Ok(canal);
        }

        // POST: api/canais
        [HttpPost]
        public async Task<ActionResult<Canal>> CriarCanal([FromBody] Canal canal)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            _contexto.Canais.Add(canal);
            await _contexto.SaveChangesAsync();

            return CreatedAtAction(nameof(ObterCanalPorId), new { id = canal.Id }, canal);
        }

        // PUT: api/canais/5
        [HttpPut("{id}")]
        public async Task<IActionResult> AtualizarCanal(int id, [FromBody] Canal canalAtualizado)
        {
            var canalExistente = await _contexto.Canais.FindAsync(id);
            if (canalExistente == null)
            {
                return NotFound(new { mensagem = $"Canal com ID {id} não encontrado." });
            }

            canalExistente.Nome = canalAtualizado.Nome;
            canalExistente.PermitePosse = canalAtualizado.PermitePosse;
            canalExistente.PermitePreco = canalAtualizado.PermitePreco;

            await _contexto.SaveChangesAsync();
            return Ok(canalExistente);
        }

        // DELETE: api/canais/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> ExcluirCanal(int id)
        {
            var canal = await _contexto.Canais.FindAsync(id);
            if (canal == null)
            {
                return NotFound(new { mensagem = $"Canal com ID {id} não encontrado." });
            }

            if (await _contexto.AcompanhamentosJogos.AnyAsync(item => item.CanalId == id))
                return Conflict(new { mensagem = "Este canal possui acompanhamento e não pode ser excluído." });

            _contexto.Canais.Remove(canal);
            await _contexto.SaveChangesAsync();

            return NoContent();
        }
    }
}
