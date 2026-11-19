using System.Linq.Expressions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using gamelife_api.Context;
using gamelife_api.Contracts;
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
        public async Task<ActionResult<IEnumerable<PosseResumoResponse>>> ObterPosses()
        {
            var posses = await _contexto.PossesJogos
                .Select(ProjetarResumo)
                .ToListAsync();

            return Ok(posses);
        }

        // GET: api/posses-jogos/5
        [HttpGet("{id}")]
        public async Task<ActionResult<PosseResumoResponse>> ObterPossePorId(int id)
        {
            var posse = await _contexto.PossesJogos
                .Where(p => p.Id == id)
                .Select(ProjetarResumo)
                .FirstOrDefaultAsync();

            if (posse == null)
            {
                return NotFound(new { mensagem = $"Posse com ID {id} não encontrada." });
            }

            return Ok(posse);
        }

        // POST: api/posses-jogos
        [HttpPost]
        public async Task<ActionResult<PosseCriadaResponse>> CriarPosse([FromBody] CriarPosseRequest dados)
        {
            var canal = await _contexto.Canais.FindAsync(dados.CanalId);
            if (canal == null)
            {
                return BadRequest(new { mensagem = $"Canal com ID {dados.CanalId} não encontrado." });
            }

            if (!canal.PermitePosse)
            {
                return BadRequest(new { mensagem = $"O canal '{canal.Nome}' não aceita posse de jogos." });
            }

            var jaPossui = await _contexto.PossesJogos.AnyAsync(p => p.JogoId == dados.JogoId && p.CanalId == dados.CanalId);
            if (jaPossui)
            {
                return BadRequest(new { mensagem = "Este jogo já está vinculado a este canal." });
            }

            if (!await _contexto.Jogos.AnyAsync(item => item.Id == dados.JogoId))
                return BadRequest(new { mensagem = $"Jogo com ID {dados.JogoId} não encontrado." });

            string? servico = null;
            if (dados.ResgatadoDeAssinatura)
            {
                try
                {
                    servico = AssinaturaCatalogo.NormalizarServico(dados.ServicoExigido);
                }
                catch (ArgumentException ex)
                {
                    return BadRequest(new { mensagem = ex.Message });
                }
            }

            var posse = new PosseJogo
            {
                JogoId = dados.JogoId,
                CanalId = dados.CanalId,
                CompraDigital = dados.CompraDigital,
                MidiaFisica = dados.MidiaFisica,
                PorAssinatura = dados.PorAssinatura || dados.ResgatadoDeAssinatura,
                ResgatadoDeAssinatura = dados.ResgatadoDeAssinatura,
                ServicoExigido = servico,
                AdquiridoEm = DateTime.UtcNow
            };
            _contexto.PossesJogos.Add(posse);
            await _contexto.SaveChangesAsync();

            return CreatedAtAction(nameof(ObterPossePorId), new { id = posse.Id }, new PosseCriadaResponse(
                posse.Id, posse.JogoId, posse.CanalId, posse.AdquiridoEm, posse.CompraDigital, posse.MidiaFisica,
                posse.PorAssinatura, posse.ResgatadoDeAssinatura, posse.ServicoExigido));
        }

        private static readonly Expression<Func<PosseJogo, PosseResumoResponse>> ProjetarResumo = p =>
            new(p.Id, p.JogoId, p.CanalId, p.AdquiridoEm,
                p.Jogo != null ? new JogoResponse(p.Jogo.Id, p.Jogo.Titulo, p.Jogo.AnoLancamento) : null,
                p.Canal != null ? new CanalIdentificacaoResponse(p.Canal.Id, p.Canal.Nome) : null);

        // DELETE: api/posses-jogos/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> ExcluirPosse(int id)
        {
            var posse = await _contexto.PossesJogos.FindAsync(id);
            if (posse == null)
            {
                return NotFound(new { mensagem = $"Posse com ID {id} não encontrada." });
            }

            var acompanhamentos = await _contexto.AcompanhamentosJogos
                .Where(item => item.PosseJogoId == id)
                .ToListAsync();
            foreach (var acompanhamento in acompanhamentos)
                acompanhamento.PosseJogoId = null;
            _contexto.PossesJogos.Remove(posse);
            await _contexto.SaveChangesAsync();

            return NoContent();
        }
    }
}
