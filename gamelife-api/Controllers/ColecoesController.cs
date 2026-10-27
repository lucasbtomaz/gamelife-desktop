using System.ComponentModel.DataAnnotations;
using gamelife_api.Context;
using gamelife_api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace gamelife_api.Controllers;

[ApiController]
[Route("api/colecoes")]
public sealed class ColecoesController : ControllerBase
{
    private readonly GameLifeDbContext _contexto;
    public ColecoesController(GameLifeDbContext contexto) => _contexto = contexto;

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ColecaoResponse>>> Obter(CancellationToken cancellationToken)
        => Ok(await Consulta().ToListAsync(cancellationToken));

    [HttpPost]
    public async Task<ActionResult<ColecaoResponse>> Criar(
        [FromBody] CriarColecaoRequest request,
        CancellationToken cancellationToken)
    {
        var colecao = new Colecao { Nome = request.Nome.Trim(), Descricao = request.Descricao?.Trim() };
        _contexto.Colecoes.Add(colecao);
        await _contexto.SaveChangesAsync(cancellationToken);
        return Created($"/api/colecoes/{colecao.Id}", new ColecaoResponse(colecao.Id, colecao.Nome, colecao.Descricao, []));
    }

    [HttpPost("{id:int}/jogos")]
    public async Task<IActionResult> AdicionarJogo(int id, [FromBody] AdicionarJogoRequest request, CancellationToken cancellationToken)
    {
        if (!await _contexto.Colecoes.AnyAsync(item => item.Id == id, cancellationToken) ||
            !await _contexto.Jogos.AnyAsync(item => item.Id == request.JogoId, cancellationToken))
            return NotFound();

        if (await _contexto.ColecoesJogos.AnyAsync(item => item.ColecaoId == id && item.JogoId == request.JogoId, cancellationToken))
            return Conflict(new { mensagem = "O jogo já pertence a esta coleção." });

        _contexto.ColecoesJogos.Add(new ColecaoJogo { ColecaoId = id, JogoId = request.JogoId });
        await _contexto.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Excluir(int id, CancellationToken cancellationToken)
    {
        var colecao = await _contexto.Colecoes.FindAsync([id], cancellationToken);
        if (colecao is null) return NotFound();
        _contexto.Colecoes.Remove(colecao);
        await _contexto.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    private IQueryable<ColecaoResponse> Consulta() => _contexto.Colecoes.AsNoTracking()
        .OrderBy(item => item.Nome)
        .Select(item => new ColecaoResponse(item.Id, item.Nome, item.Descricao,
            item.Jogos.OrderBy(vinculo => vinculo.Jogo.Titulo)
                .Select(vinculo => new JogoColecaoResponse(vinculo.JogoId, vinculo.Jogo.Titulo)).ToList()));
}

public sealed record CriarColecaoRequest([Required, MaxLength(100)] string Nome, [MaxLength(500)] string? Descricao);
public sealed record AdicionarJogoRequest(int JogoId);
public sealed record ColecaoResponse(int Id, string Nome, string? Descricao, IReadOnlyList<JogoColecaoResponse> Jogos);
public sealed record JogoColecaoResponse(int Id, string Titulo);
