using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using gamelife_api.Context;
using gamelife_api.Models;

namespace gamelife_api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class JogosController : ControllerBase
    {
        private readonly GameLifeDbContext _contexto;

        public JogosController(GameLifeDbContext contexto)
        {
            _contexto = contexto;
        }

        // GET: api/jogos
        [HttpGet]
        public async Task<ActionResult<IEnumerable<object>>> ObterJogos([FromQuery] string? search)
        {
            var consulta = _contexto.Jogos.AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                consulta = consulta.Where(j => j.Titulo.ToLower().Contains(search.ToLower()));
            }

            var jogos = await consulta
                .Select(j => new
                {
                    j.Id,
                    j.Titulo,
                    j.AnoLancamento,
                    PossesCount = _contexto.PossesJogos.Count(p => p.JogoId == j.Id),
                    NaListaDesejos = _contexto.ItensListaDesejos.Any(d => d.JogoId == j.Id)
                })
                .ToListAsync();

            return Ok(jogos);
        }

        // GET: api/jogos/5
        [HttpGet("{id}")]
        public async Task<ActionResult<object>> ObterJogoPorId(int id)
        {
            var jogo = await _contexto.Jogos
                .Include(j => j.PossesJogos)
                    .ThenInclude(p => p.Canal)
                .FirstOrDefaultAsync(j => j.Id == id);

            if (jogo == null)
            {
                return NotFound(new { mensagem = $"Jogo com ID {id} não encontrado." });
            }

            var itemListaDesejos = await _contexto.ItensListaDesejos
                .Include(i => i.Motivos)
                .Include(i => i.RegistrosPrecos)
                .FirstOrDefaultAsync(i => i.JogoId == id);

            return Ok(new
            {
                jogo.Id,
                jogo.Titulo,
                jogo.AnoLancamento,
                Posses = jogo.PossesJogos.Select(p => new
                {
                    p.Id,
                    p.CanalId,
                    CanalNome = p.Canal != null ? p.Canal.Nome : null,
                    p.AdquiridoEm
                }),
                ItemListaDesejos = itemListaDesejos != null ? new
                {
                    itemListaDesejos.Id,
                    itemListaDesejos.PrecoDesejado,
                    itemListaDesejos.Observacao,
                    itemListaDesejos.CriadoEm,
                    Motivos = itemListaDesejos.Motivos.Select(m => new { m.Id, m.Descricao }),
                    Precos = itemListaDesejos.RegistrosPrecos.Select(r => new { r.Id, r.CanalId, r.PrecoCheio, r.PrecoAtual, r.Disponivel })
                } : null
            });
        }

        // POST: api/jogos
        [HttpPost]
        public async Task<ActionResult<Jogo>> CriarJogo([FromBody] Jogo jogo)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            _contexto.Jogos.Add(jogo);
            await _contexto.SaveChangesAsync();

            return CreatedAtAction(nameof(ObterJogoPorId), new { id = jogo.Id }, jogo);
        }

        // PUT: api/jogos/5
        [HttpPut("{id}")]
        public async Task<IActionResult> AtualizarJogo(int id, [FromBody] Jogo dadosAtualizados)
        {
            if (id != dadosAtualizados.Id && dadosAtualizados.Id != 0)
            {
                return BadRequest(new { mensagem = "O ID informado na rota não corresponde ao objeto." });
            }

            var jogoExistente = await _contexto.Jogos.FindAsync(id);
            if (jogoExistente == null)
            {
                return NotFound(new { mensagem = $"Jogo com ID {id} não encontrado." });
            }

            jogoExistente.Titulo = dadosAtualizados.Titulo;
            jogoExistente.AnoLancamento = dadosAtualizados.AnoLancamento;

            try
            {
                await _contexto.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_contexto.Jogos.Any(j => j.Id == id))
                {
                    return NotFound();
                }
                throw;
            }

            return Ok(jogoExistente);
        }

        // DELETE: api/jogos/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> ExcluirJogo(int id)
        {
            var jogo = await _contexto.Jogos.FindAsync(id);
            if (jogo == null)
            {
                return NotFound(new { mensagem = $"Jogo com ID {id} não encontrado." });
            }

            _contexto.Jogos.Remove(jogo);
            await _contexto.SaveChangesAsync();

            return NoContent();
        }
    }
}
