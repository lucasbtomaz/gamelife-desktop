using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using gamelife_api.Context;
using gamelife_api.Models;

namespace gamelife_api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Route("api/precos")]
    public class RegistrosPrecosController : ControllerBase
    {
        private readonly GameLifeDbContext _contexto;

        public RegistrosPrecosController(GameLifeDbContext contexto)
        {
            _contexto = contexto;
        }

        // GET: api/registros-precos
        [HttpGet]
        public async Task<ActionResult<IEnumerable<object>>> ObterRegistrosPrecos()
        {
            var precos = await _contexto.RegistrosPrecos
                .Include(r => r.Canal)
                .Include(r => r.ItemListaDesejos)
                    .ThenInclude(i => i.Jogo)
                .Select(r => new
                {
                    r.Id,
                    r.ItemListaDesejosId,
                    r.CanalId,
                    r.PrecoCheio,
                    r.PrecoAtual,
                    r.Disponivel,
                    r.ConsultadoEm,
                    CanalNome = r.Canal != null ? r.Canal.Nome : null,
                    JogoTitulo = r.ItemListaDesejos != null && r.ItemListaDesejos.Jogo != null ? r.ItemListaDesejos.Jogo.Titulo : null
                })
                .ToListAsync();

            return Ok(precos);
        }

        // POST: api/registros-precos
        [HttpPost]
        public async Task<ActionResult<RegistroPreco>> CriarRegistroPreco([FromBody] RegistroPreco registro)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var canal = await _contexto.Canais.FindAsync(registro.CanalId);
            if (canal == null)
            {
                return BadRequest(new { mensagem = $"Canal com ID {registro.CanalId} não encontrado." });
            }

            if (!canal.PermitePreco)
            {
                return BadRequest(new { mensagem = $"O canal '{canal.Nome}' não aceita cotação de preço." });
            }

            var itemExiste = await _contexto.ItensListaDesejos.AnyAsync(i => i.Id == registro.ItemListaDesejosId);
            if (!itemExiste)
            {
                return BadRequest(new { mensagem = $"Item da lista de desejos #{registro.ItemListaDesejosId} não encontrado." });
            }

            if (registro.ConsultadoEm == default)
            {
                registro.ConsultadoEm = DateTime.UtcNow;
            }

            _contexto.RegistrosPrecos.Add(registro);
            await _contexto.SaveChangesAsync();

            return Ok(registro);
        }

        // PUT: api/registros-precos/5
        [HttpPut("{id}")]
        public async Task<IActionResult> AtualizarRegistroPreco(int id, [FromBody] RegistroPreco atualizado)
        {
            var existente = await _contexto.RegistrosPrecos.FindAsync(id);
            if (existente == null)
            {
                return NotFound(new { mensagem = $"Registro com ID {id} não encontrado." });
            }

            existente.PrecoCheio = atualizado.PrecoCheio;
            existente.PrecoAtual = atualizado.PrecoAtual;
            existente.Disponivel = atualizado.Disponivel;
            existente.ConsultadoEm = DateTime.UtcNow;

            await _contexto.SaveChangesAsync();
            return Ok(existente);
        }

        // DELETE: api/registros-precos/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> ExcluirRegistroPreco(int id)
        {
            var reg = await _contexto.RegistrosPrecos.FindAsync(id);
            if (reg == null)
            {
                return NotFound(new { mensagem = $"Registro com ID {id} não encontrado." });
            }

            _contexto.RegistrosPrecos.Remove(reg);
            await _contexto.SaveChangesAsync();

            return NoContent();
        }
    }
}
