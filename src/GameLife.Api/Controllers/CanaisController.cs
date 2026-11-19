using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using gamelife_api.Context;
using gamelife_api.Contracts;
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
        public async Task<ActionResult<IEnumerable<CanalResumoResponse>>> ObterCanais()
        {
            var canais = await _contexto.Canais
                .Select(c => new CanalResumoResponse(c.Id, c.Nome, c.PermitePosse, c.PermitePreco,
                    _contexto.PossesJogos.Count(p => p.CanalId == c.Id)))
                .ToListAsync();

            return Ok(canais);
        }

        // GET: api/canais/5
        [HttpGet("{id}")]
        public async Task<ActionResult<CanalResponse>> ObterCanalPorId(int id)
        {
            var canal = await _contexto.Canais.FindAsync(id);
            if (canal == null)
            {
                return NotFound(new { mensagem = $"Canal com ID {id} não encontrado." });
            }

            return Ok(Projetar(canal));
        }

        // POST: api/canais
        [HttpPost]
        public async Task<ActionResult<CanalResponse>> CriarCanal([FromBody] SalvarCanalRequest dados)
        {
            var canal = new Canal
            {
                Nome = dados.Nome,
                PermitePosse = dados.PermitePosse,
                PermitePreco = dados.PermitePreco
            };
            _contexto.Canais.Add(canal);
            await _contexto.SaveChangesAsync();

            return CreatedAtAction(nameof(ObterCanalPorId), new { id = canal.Id }, Projetar(canal));
        }

        // PUT: api/canais/5
        [HttpPut("{id}")]
        public async Task<IActionResult> AtualizarCanal(int id, [FromBody] SalvarCanalRequest canalAtualizado)
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
            return Ok(Projetar(canalExistente));
        }

        private static CanalResponse Projetar(Canal canal) =>
            new(canal.Id, canal.Nome, canal.PermitePosse, canal.PermitePreco);

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
