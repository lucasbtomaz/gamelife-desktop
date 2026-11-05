using gamelife_api.Context;
using gamelife_api.Contracts;
using gamelife_api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text;

namespace gamelife_api.Controllers;

[ApiController]
[Route("api/acompanhamentos-jogos")]
public sealed class AcompanhamentosJogosController : ControllerBase
{
    private static readonly SemaphoreSlim EscritaGate = new(1, 1);
    private readonly GameLifeDbContext _contexto;

    public AcompanhamentosJogosController(GameLifeDbContext contexto) => _contexto = contexto;

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<AcompanhamentoJogoResponse>>> Obter(CancellationToken cancellationToken)
    {
        var registros = await _contexto.AcompanhamentosJogos.AsNoTracking()
            .Include(item => item.Jogo)
            .Include(item => item.Canal)
            .OrderBy(item => item.Jogo.Titulo).ThenBy(item => item.Canal.Nome)
            .ToListAsync(cancellationToken);
        var posses = await _contexto.PossesJogos.AsNoTracking()
            .Select(item => new { item.JogoId, item.CanalId })
            .ToListAsync(cancellationToken);
        var chavesPosses = posses.Select(item => (item.JogoId, item.CanalId)).ToHashSet();
        return Ok(registros.Select(item => Projetar(item, chavesPosses.Contains((item.JogoId, item.CanalId)))).ToList());
    }

    [HttpGet("resumo")]
    public async Task<ActionResult<ResumoAcompanhamentosResponse>> ObterResumo(CancellationToken cancellationToken)
    {
        var grupos = await _contexto.AcompanhamentosJogos.AsNoTracking()
            .GroupBy(item => item.Status)
            .Select(grupo => new { Status = grupo.Key, Quantidade = grupo.Count() })
            .ToListAsync(cancellationToken);
        var total = grupos.Sum(item => item.Quantidade);
        return Ok(new ResumoAcompanhamentosResponse(
            total,
            grupos.FirstOrDefault(item => item.Status == StatusAcompanhamentoJogo.Zerado)?.Quantidade ?? 0,
            grupos.FirstOrDefault(item => item.Status == StatusAcompanhamentoJogo.Dropado)?.Quantidade ?? 0,
            grupos.FirstOrDefault(item => item.Status == StatusAcompanhamentoJogo.NaoTerminado)?.Quantidade ?? 0));
    }

    [HttpPost]
    public async Task<ActionResult<AcompanhamentoJogoResponse>> Criar(
        [FromBody] SalvarAcompanhamentoJogoRequest request,
        CancellationToken cancellationToken)
    {
        var validacao = Validar(request);
        if (validacao is not null) return validacao;
        await EscritaGate.WaitAsync(cancellationToken);
        try
        {
            var (jogo, canal) = await ObterOuCriarReferencias(request, cancellationToken);
            if (await ExisteCombinacao(jogo.Id, canal.Id, null, cancellationToken))
                return Conflict(new ProblemDetails { Detail = "Este jogo já está sendo acompanhado nesta plataforma." });

            var acompanhamento = new AcompanhamentoJogo { Jogo = jogo, Canal = canal };
            await Aplicar(acompanhamento, request, jogo, canal, cancellationToken);
            _contexto.AcompanhamentosJogos.Add(acompanhamento);
            try
            {
                await _contexto.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException)
            {
                return Conflict(new ProblemDetails { Detail = "Este jogo já está sendo acompanhado nesta plataforma." });
            }
            return Created($"/api/acompanhamentos-jogos/{acompanhamento.Id}", Responder(acompanhamento));
        }
        finally
        {
            EscritaGate.Release();
        }
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<AcompanhamentoJogoResponse>> Atualizar(
        int id,
        [FromBody] SalvarAcompanhamentoJogoRequest request,
        CancellationToken cancellationToken)
    {
        var validacao = Validar(request);
        if (validacao is not null) return validacao;
        await EscritaGate.WaitAsync(cancellationToken);
        try
        {
            var acompanhamento = await _contexto.AcompanhamentosJogos
                .Include(item => item.Jogo).Include(item => item.Canal).Include(item => item.PosseJogo)
                .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
            if (acompanhamento is null) return NotFound();

            var (jogo, canal) = await ObterOuCriarReferencias(request, cancellationToken);
            if (await ExisteCombinacao(jogo.Id, canal.Id, id, cancellationToken))
                return Conflict(new ProblemDetails { Detail = "Este jogo já está sendo acompanhado nesta plataforma." });

            await Aplicar(acompanhamento, request, jogo, canal, cancellationToken);
            try
            {
                await _contexto.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException)
            {
                return Conflict(new ProblemDetails { Detail = "Este jogo já está sendo acompanhado nesta plataforma." });
            }
            return Ok(Responder(acompanhamento));
        }
        finally
        {
            EscritaGate.Release();
        }
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Excluir(int id, CancellationToken cancellationToken)
    {
        var acompanhamento = await _contexto.AcompanhamentosJogos.FindAsync([id], cancellationToken);
        if (acompanhamento is null) return NotFound();
        _contexto.AcompanhamentosJogos.Remove(acompanhamento);
        await _contexto.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    private ActionResult? Validar(SalvarAcompanhamentoJogoRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Titulo) || string.IsNullOrWhiteSpace(request.Plataforma))
            return BadRequest(new ProblemDetails { Detail = "Informe o jogo e a plataforma." });
        if (!Enum.TryParse<StatusAcompanhamentoJogo>(request.Status, true, out var status) ||
            !Enum.IsDefined(status) || int.TryParse(request.Status, out _))
            return BadRequest(new ProblemDetails { Detail = "Informe um status válido." });
        if (request.ConquistasObtidas.HasValue && !request.ConquistasPossiveis.HasValue)
            return BadRequest(new ProblemDetails { Detail = "Informe as conquistas possíveis." });
        if (request.ConquistasObtidas > request.ConquistasPossiveis)
            return BadRequest(new ProblemDetails { Detail = "Conquistas obtidas não podem superar as possíveis." });
        return null;
    }

    private async Task<(Jogo Jogo, Canal Canal)> ObterOuCriarReferencias(
        SalvarAcompanhamentoJogoRequest request,
        CancellationToken cancellationToken)
    {
        var titulo = request.Titulo.Trim();
        var plataforma = request.Plataforma.Trim();
        var tituloNormalizado = NormalizarChave(titulo);
        var plataformaNormalizada = NormalizarChave(plataforma);
        var jogos = await _contexto.Jogos.ToListAsync(cancellationToken);
        var canais = await _contexto.Canais.ToListAsync(cancellationToken);
        var jogo = jogos.FirstOrDefault(item => NormalizarChave(item.Titulo) == tituloNormalizado)
            ?? new Jogo { Titulo = titulo };
        var canal = canais.FirstOrDefault(item => NormalizarChave(item.Nome) == plataformaNormalizada)
            ?? new Canal { Nome = plataforma, PermitePosse = request.AdicionarBiblioteca };
        if (jogo.Id == 0) _contexto.Jogos.Add(jogo);
        if (canal.Id == 0) _contexto.Canais.Add(canal);
        if (request.AdicionarBiblioteca) canal.PermitePosse = true;
        return (jogo, canal);
    }

    private Task<bool> ExisteCombinacao(int jogoId, int canalId, int? ignorarId, CancellationToken cancellationToken) =>
        _contexto.AcompanhamentosJogos.AnyAsync(
            item => item.JogoId == jogoId && item.CanalId == canalId && (!ignorarId.HasValue || item.Id != ignorarId),
            cancellationToken);

    private async Task Aplicar(
        AcompanhamentoJogo acompanhamento,
        SalvarAcompanhamentoJogoRequest request,
        Jogo jogo,
        Canal canal,
        CancellationToken cancellationToken)
    {
        acompanhamento.Jogo = jogo;
        acompanhamento.JogoId = jogo.Id;
        acompanhamento.Canal = canal;
        acompanhamento.CanalId = canal.Id;
        acompanhamento.Status = Enum.Parse<StatusAcompanhamentoJogo>(request.Status, true);
        acompanhamento.DataStatus = request.DataStatus;
        acompanhamento.TempoEmHoras = request.TempoEmHoras;
        acompanhamento.ConquistasPossiveis = request.ConquistasPossiveis;
        acompanhamento.ConquistasObtidas = request.ConquistasObtidas;

        var posse = jogo.Id == 0 || canal.Id == 0
            ? null
            : await _contexto.PossesJogos.FirstOrDefaultAsync(
                item => item.JogoId == jogo.Id && item.CanalId == canal.Id, cancellationToken);
        if (posse is null && request.AdicionarBiblioteca)
        {
            posse = new PosseJogo { Jogo = jogo, Canal = canal, MidiaFisica = false, AdquiridoEm = DateTime.UtcNow };
            _contexto.PossesJogos.Add(posse);
        }
        acompanhamento.PosseJogo = posse;
    }

    private static AcompanhamentoJogoResponse Responder(AcompanhamentoJogo item) => new(
        item.Id, item.JogoId, item.Jogo.Titulo, item.CanalId, item.Canal.Nome, item.Status.ToString(), item.DataStatus,
        item.TempoEmHoras, item.ConquistasPossiveis, item.ConquistasObtidas, CalcularPercentual(item), item.PosseJogoId.HasValue || item.PosseJogo is not null);

    private static AcompanhamentoJogoResponse Projetar(AcompanhamentoJogo item, bool estaNaBiblioteca) => new(
        item.Id, item.JogoId, item.Jogo.Titulo, item.CanalId, item.Canal.Nome, item.Status.ToString(), item.DataStatus,
        item.TempoEmHoras, item.ConquistasPossiveis, item.ConquistasObtidas, CalcularPercentual(item), estaNaBiblioteca);

    private static decimal CalcularPercentual(AcompanhamentoJogo item) => item.ConquistasPossiveis > 0
        ? Math.Round((item.ConquistasObtidas ?? 0) * 100m / item.ConquistasPossiveis.Value, 0, MidpointRounding.AwayFromZero)
        : 0;

    public static string NormalizarChave(string valor) =>
        valor.Trim().Normalize(NormalizationForm.FormKC).ToUpperInvariant();
}
