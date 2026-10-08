using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using gamelife_api.Context;
using gamelife_api.Models;

namespace gamelife_api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Route("api/registros")]
    public class RegistrosJogosController : ControllerBase
    {
        private readonly GameLifeDbContext _contexto;

        public RegistrosJogosController(GameLifeDbContext contexto)
        {
            _contexto = contexto;
        }

        // GET: api/registros-jogos
        [HttpGet]
        public async Task<ActionResult<IEnumerable<object>>> ObterRegistros()
        {
            var registros = await _contexto.RegistrosJogos
                .Include(r => r.PosseJogo)
                    .ThenInclude(p => p.Jogo)
                .Include(r => r.PosseJogo)
                    .ThenInclude(p => p.Canal)
                .Select(r => new
                {
                    r.Id,
                    r.PosseJogoId,
                    r.FinalizadoEm,
                    r.TempoEmHoras,
                    r.Dificuldade,
                    r.ConquistasPossiveis,
                    r.ConquistasObtidas,
                    JogoTitulo = r.PosseJogo != null && r.PosseJogo.Jogo != null ? r.PosseJogo.Jogo.Titulo : null,
                    CanalNome = r.PosseJogo != null && r.PosseJogo.Canal != null ? r.PosseJogo.Canal.Nome : null
                })
                .ToListAsync();

            return Ok(registros);
        }

        // GET: api/registros-jogos/5
        [HttpGet("{id}")]
        public async Task<ActionResult<object>> ObterRegistroPorId(int id)
        {
            var reg = await _contexto.RegistrosJogos
                .Include(r => r.PosseJogo)
                    .ThenInclude(p => p.Jogo)
                .Include(r => r.PosseJogo)
                    .ThenInclude(p => p.Canal)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (reg == null)
            {
                return NotFound(new { mensagem = $"Registro com ID {id} não encontrado." });
            }

            return Ok(reg);
        }

        // POST: api/registros-jogos
        [HttpPost]
        public async Task<ActionResult<RegistroJogo>> CriarRegistro([FromBody] RegistroJogo registro)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var posseExiste = await _contexto.PossesJogos.AnyAsync(p => p.Id == registro.PosseJogoId);
            if (!posseExiste)
            {
                return BadRequest(new { mensagem = $"Posse com ID {registro.PosseJogoId} não encontrada." });
            }

            _contexto.RegistrosJogos.Add(registro);
            await _contexto.SaveChangesAsync();

            return CreatedAtAction(nameof(ObterRegistroPorId), new { id = registro.Id }, registro);
        }

        // PUT: api/registros-jogos/5
        [HttpPut("{id}")]
        public async Task<IActionResult> AtualizarRegistro(int id, [FromBody] RegistroJogo atualizado)
        {
            var existente = await _contexto.RegistrosJogos.FindAsync(id);
            if (existente == null)
            {
                return NotFound(new { mensagem = $"Registro com ID {id} não encontrado." });
            }

            existente.TempoEmHoras = atualizado.TempoEmHoras;
            existente.FinalizadoEm = atualizado.FinalizadoEm;
            existente.Dificuldade = atualizado.Dificuldade;
            existente.ConquistasPossiveis = atualizado.ConquistasPossiveis;
            existente.ConquistasObtidas = atualizado.ConquistasObtidas;

            await _contexto.SaveChangesAsync();
            return Ok(existente);
        }

        // DELETE: api/registros-jogos/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> ExcluirRegistro(int id)
        {
            var reg = await _contexto.RegistrosJogos.FindAsync(id);
            if (reg == null)
            {
                return NotFound(new { mensagem = $"Registro com ID {id} não encontrado." });
            }

            _contexto.RegistrosJogos.Remove(reg);
            await _contexto.SaveChangesAsync();

            return NoContent();
        }
    }
}
