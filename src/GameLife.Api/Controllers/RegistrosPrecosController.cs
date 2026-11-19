using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using gamelife_api.Context;
using gamelife_api.Contracts;
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
        public async Task<ActionResult<IEnumerable<PrecoResumoResponse>>> ObterRegistrosPrecos()
        {
            var precos = await _contexto.RegistrosPrecos
                .Select(r => new PrecoResumoResponse(
                    r.Id, r.ItemListaDesejosId, r.CanalId, r.PrecoCheio, r.PrecoAtual, r.Disponivel, r.ConsultadoEm,
                    r.Canal != null ? r.Canal.Nome : null,
                    r.ItemListaDesejos != null && r.ItemListaDesejos.Jogo != null ? r.ItemListaDesejos.Jogo.Titulo : null))
                .ToListAsync();

            return Ok(precos);
        }

        // POST: api/registros-precos
        [HttpPost]
        public async Task<ActionResult<PrecoResponse>> CriarRegistroPreco([FromBody] CriarPrecoRequest dados)
        {
            var canal = await _contexto.Canais.FindAsync(dados.CanalId);
            if (canal == null)
            {
                return BadRequest(new { mensagem = $"Canal com ID {dados.CanalId} não encontrado." });
            }

            if (!canal.PermitePreco)
            {
                return BadRequest(new { mensagem = $"O canal '{canal.Nome}' não aceita cotação de preço." });
            }

            var itemExiste = await _contexto.ItensListaDesejos.AnyAsync(i => i.Id == dados.ItemListaDesejosId);
            if (!itemExiste)
            {
                return BadRequest(new { mensagem = $"Item da lista de desejos #{dados.ItemListaDesejosId} não encontrado." });
            }

            var registro = new RegistroPreco
            {
                ItemListaDesejosId = dados.ItemListaDesejosId,
                CanalId = dados.CanalId,
                PrecoCheio = dados.PrecoCheio,
                PrecoAtual = dados.PrecoAtual,
                Disponivel = dados.Disponivel,
                ConsultadoEm = DateTime.UtcNow
            };
            _contexto.RegistrosPrecos.Add(registro);
            await _contexto.SaveChangesAsync();

            return Ok(Projetar(registro));
        }

        // PUT: api/registros-precos/5
        [HttpPut("{id}")]
        public async Task<IActionResult> AtualizarRegistroPreco(int id, [FromBody] AtualizarPrecoRequest atualizado)
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
            return Ok(Projetar(existente));
        }

        private static PrecoResponse Projetar(RegistroPreco registro) =>
            new(registro.Id, registro.ItemListaDesejosId, registro.CanalId, registro.PrecoCheio,
                registro.PrecoAtual, registro.Disponivel, registro.ConsultadoEm);

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
