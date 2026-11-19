using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using gamelife_api.Context;
using gamelife_api.Contracts;
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
        public async Task<ActionResult<IEnumerable<JogoResumoResponse>>> ObterJogos([FromQuery] string? search)
        {
            var consulta = _contexto.Jogos.AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                consulta = consulta.Where(j => j.Titulo.ToLower().Contains(search.ToLower()));
            }

            var jogos = await consulta
                .Select(j => new JogoResumoResponse(j.Id, j.Titulo, j.AnoLancamento,
                    _contexto.PossesJogos.Count(p => p.JogoId == j.Id),
                    _contexto.ItensListaDesejos.Any(d => d.JogoId == j.Id)))
                .ToListAsync();

            return Ok(jogos);
        }

        // GET: api/jogos/5
        [HttpGet("{id}")]
        public async Task<ActionResult<JogoDetalheResponse>> ObterJogoPorId(int id)
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

            return Ok(new JogoDetalheResponse(jogo.Id, jogo.Titulo, jogo.AnoLancamento,
                jogo.PossesJogos.Select(p => new JogoPosseResponse(p.Id, p.CanalId, p.Canal?.Nome, p.AdquiridoEm)).ToArray(),
                itemListaDesejos is null ? null : new JogoDesejoResponse(
                    itemListaDesejos.Id, itemListaDesejos.PrecoDesejado, itemListaDesejos.Observacao,
                    itemListaDesejos.CriadoEm,
                    itemListaDesejos.Motivos.Select(m => new JogoMotivoResponse(m.Id, m.Descricao)).ToArray(),
                    itemListaDesejos.RegistrosPrecos.Select(r =>
                        new JogoPrecoResponse(r.Id, r.CanalId, r.PrecoCheio, r.PrecoAtual, r.Disponivel)).ToArray())));
        }

        // POST: api/jogos
        [HttpPost]
        public async Task<ActionResult<JogoResponse>> CriarJogo([FromBody] CriarJogoRequest dados)
        {
            var tituloNormalizado = BibliotecaController.NormalizarTitulo(dados.Titulo);
            var jogosExistentes = await _contexto.Jogos.ToListAsync();
            var duplicado = jogosExistentes.FirstOrDefault(j => BibliotecaController.NormalizarTitulo(j.Titulo) == tituloNormalizado);

            if (duplicado != null)
            {
                return Conflict(new { erro = $"O jogo \"{duplicado.Titulo}\" já está cadastrado. Variações apenas de maiúsculas/minúsculas, espaços ou pontuação não são permitidas." });
            }

            var jogo = new Jogo { Titulo = dados.Titulo, AnoLancamento = dados.AnoLancamento };
            _contexto.Jogos.Add(jogo);
            await _contexto.SaveChangesAsync();

            return CreatedAtAction(nameof(ObterJogoPorId), new { id = jogo.Id }, new JogoResponse(jogo.Id, jogo.Titulo, jogo.AnoLancamento));
        }

        // PUT: api/jogos/5
        [HttpPut("{id}")]
        public async Task<IActionResult> AtualizarJogo(int id, [FromBody] AtualizarJogoRequest dadosAtualizados)
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

            return Ok(new JogoResponse(jogoExistente.Id, jogoExistente.Titulo, jogoExistente.AnoLancamento));
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

            if (await _contexto.AcompanhamentosJogos.AnyAsync(item => item.JogoId == id))
                return Conflict(new { mensagem = "Este jogo possui acompanhamento e não pode ser excluído." });

            _contexto.Jogos.Remove(jogo);
            await _contexto.SaveChangesAsync();

            return NoContent();
        }
    }
}
