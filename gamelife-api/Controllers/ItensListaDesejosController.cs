using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using gamelife_api.Context;
using gamelife_api.Models;

namespace gamelife_api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Route("api/lista-desejos")]
    public class ItensListaDesejosController : ControllerBase
    {
        private readonly GameLifeDbContext _contexto;

        public ItensListaDesejosController(GameLifeDbContext contexto)
        {
            _contexto = contexto;
        }

        // GET: api/lista-desejos
        [HttpGet]
        public async Task<ActionResult<IEnumerable<object>>> ObterItensListaDesejos()
        {
            var itens = await _contexto.ItensListaDesejos
                .Include(i => i.Jogo)
                .Include(i => i.Motivos)
                .Include(i => i.RegistrosPrecos)
                    .ThenInclude(r => r.Canal)
                .Select(i => new
                {
                    i.Id,
                    i.JogoId,
                    i.PrecoDesejado,
                    i.Observacao,
                    i.CriadoEm,
                    Jogo = i.Jogo != null ? new { i.Jogo.Id, i.Jogo.Titulo, i.Jogo.AnoLancamento } : null,
                    Motivos = i.Motivos.Select(m => new { m.Id, m.Descricao }),
                    RegistrosPrecos = i.RegistrosPrecos.Select(r => new
                    {
                        r.Id,
                        r.CanalId,
                        CanalNome = r.Canal != null ? r.Canal.Nome : null,
                        r.PrecoCheio,
                        r.PrecoAtual,
                        r.Disponivel,
                        r.ConsultadoEm
                    })
                })
                .ToListAsync();

            return Ok(itens);
        }

        // GET: api/lista-desejos/5
        [HttpGet("{id}")]
        public async Task<ActionResult<object>> ObterItemPorId(int id)
        {
            var item = await _contexto.ItensListaDesejos
                .Include(i => i.Jogo)
                .Include(i => i.Motivos)
                .Include(i => i.RegistrosPrecos)
                    .ThenInclude(r => r.Canal)
                .FirstOrDefaultAsync(i => i.Id == id);

            if (item == null)
            {
                return NotFound(new { mensagem = $"Item com ID {id} não encontrado." });
            }

            return Ok(item);
        }

        // POST: api/lista-desejos
        [HttpPost]
        public async Task<ActionResult<ItemListaDesejos>> AdicionarItem([FromBody] ItemListaDesejos item)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var jogo = await _contexto.Jogos.FindAsync(item.JogoId);
            if (jogo == null)
            {
                return BadRequest(new { mensagem = $"Jogo com ID {item.JogoId} não encontrado." });
            }

            var jaExiste = await _contexto.ItensListaDesejos.AnyAsync(i => i.JogoId == item.JogoId);
            if (jaExiste)
            {
                return BadRequest(new { mensagem = "Este jogo já está na lista de desejos." });
            }

            item.CriadoEm = DateTime.UtcNow;
            _contexto.ItensListaDesejos.Add(item);
            await _contexto.SaveChangesAsync();

            return CreatedAtAction(nameof(ObterItemPorId), new { id = item.Id }, item);
        }

        // PUT: api/lista-desejos/5
        [HttpPut("{id}")]
        public async Task<IActionResult> AtualizarItem(int id, [FromBody] ItemListaDesejos atualizado)
        {
            var existente = await _contexto.ItensListaDesejos.FindAsync(id);
            if (existente == null)
            {
                return NotFound(new { mensagem = $"Item com ID {id} não encontrado." });
            }

            existente.PrecoDesejado = atualizado.PrecoDesejado;
            existente.Observacao = atualizado.Observacao;

            await _contexto.SaveChangesAsync();
            return Ok(existente);
        }

        // DELETE: api/lista-desejos/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> ExcluirItem(int id)
        {
            var item = await _contexto.ItensListaDesejos.FindAsync(id);
            if (item == null)
            {
                return NotFound(new { mensagem = $"Item com ID {id} não encontrado." });
            }

            _contexto.ItensListaDesejos.Remove(item);
            await _contexto.SaveChangesAsync();

            return NoContent();
        }
    }
}
