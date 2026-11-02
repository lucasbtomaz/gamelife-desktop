using System.ComponentModel.DataAnnotations;
using gamelife_api.Context;
using gamelife_api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace gamelife_api.Controllers;

[ApiController]
[Route("api/conquistas")]
public sealed class ConquistasController : ControllerBase
{
    private readonly GameLifeDbContext _contexto;
    public ConquistasController(GameLifeDbContext contexto) => _contexto = contexto;

    [HttpGet]
    public async Task<ActionResult<object>> Obter(CancellationToken cancellationToken)
        => Ok(await _contexto.Conquistas.AsNoTracking().OrderByDescending(item => item.DesbloqueadaEm)
            .Select(item => new { item.Id, item.Titulo, item.Descricao, item.Tipo, item.JogoId, Jogo = item.Jogo != null ? item.Jogo.Titulo : null, item.DesbloqueadaEm })
            .ToListAsync(cancellationToken));

    [HttpPost]
    public async Task<ActionResult<object>> Criar([FromBody] CriarConquistaRequest request, CancellationToken cancellationToken)
    {
        if (request.JogoId.HasValue && !await _contexto.Jogos.AnyAsync(item => item.Id == request.JogoId, cancellationToken))
            return BadRequest(new { mensagem = "O jogo informado não existe." });

        var conquista = new Conquista
        {
            Titulo = request.Titulo.Trim(),
            Descricao = request.Descricao?.Trim(),
            Tipo = request.Tipo.Trim(),
            JogoId = request.JogoId
        };
        _contexto.Conquistas.Add(conquista);
        await _contexto.SaveChangesAsync(cancellationToken);
        return Created($"/api/conquistas/{conquista.Id}", new { conquista.Id, conquista.Titulo, conquista.Descricao, conquista.Tipo, conquista.JogoId, conquista.DesbloqueadaEm });
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Excluir(int id, CancellationToken cancellationToken)
    {
        var conquista = await _contexto.Conquistas.FindAsync([id], cancellationToken);
        if (conquista is null) return NotFound();
        _contexto.Conquistas.Remove(conquista);
        await _contexto.SaveChangesAsync(cancellationToken);
        return NoContent();
    }
}

public sealed record CriarConquistaRequest(
    [Required, MaxLength(150)] string Titulo,
    [MaxLength(500)] string? Descricao,
    [Required, MaxLength(30)] string Tipo,
    int? JogoId);
