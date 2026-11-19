using System.Linq.Expressions;
using gamelife_api.Context;
using gamelife_api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace gamelife_api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Route("api/registros")]
[Route("api/registros-jogos")]
public sealed class RegistrosJogosController(GameLifeDbContext contexto) : ControllerBase
{
    private static readonly Expression<Func<RegistroJogo, RegistroLegadoResponse>> Projetar = item =>
        new(item.Id, item.PosseJogoId, item.FinalizadoEm, item.TempoEmHoras, item.Dificuldade,
            item.ConquistasPossiveis, item.ConquistasObtidas, item.PosseJogo.Jogo.Titulo, item.PosseJogo.Canal.Nome);

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<RegistroLegadoResponse>>> ObterRegistros(CancellationToken cancellationToken)
        => Ok(await contexto.RegistrosJogos.AsNoTracking().Select(Projetar).ToListAsync(cancellationToken));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<RegistroLegadoResponse>> ObterRegistroPorId(int id, CancellationToken cancellationToken)
    {
        var registro = await contexto.RegistrosJogos.AsNoTracking().Where(item => item.Id == id)
            .Select(Projetar).SingleOrDefaultAsync(cancellationToken);
        return registro is null
            ? NotFound(new { mensagem = $"Registro com ID {id} não encontrado." })
            : Ok(registro);
    }

    [HttpPost]
    [HttpPut("{id:int}")]
    [HttpDelete("{id:int}")]
    public IActionResult EscritaDesativada() => Problem(
        statusCode: StatusCodes.Status410Gone,
        detail: "O histórico antigo está disponível para consulta. Use /api/acompanhamentos-jogos para registrar o progresso.");
}

public sealed record RegistroLegadoResponse(
    int Id, int PosseJogoId, DateTime? FinalizadoEm, decimal? TempoEmHoras, string? Dificuldade,
    int? ConquistasPossiveis, int? ConquistasObtidas, string? JogoTitulo, string? CanalNome);
