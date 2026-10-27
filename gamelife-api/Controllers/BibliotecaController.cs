using System.ComponentModel.DataAnnotations;
using gamelife_api.Context;
using gamelife_api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace gamelife_api.Controllers;

[ApiController]
[Route("biblioteca")]
public sealed class BibliotecaController : ControllerBase
{
    private readonly GameLifeDbContext _contexto;

    public BibliotecaController(GameLifeDbContext contexto)
    {
        _contexto = contexto;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ItemBibliotecaResponse>>> ObterItens(
        CancellationToken cancellationToken)
    {
        var itens = await _contexto.PossesJogos
            .AsNoTracking()
            .Select(posse => new ItemBibliotecaResponse(
                posse.Id,
                posse.Jogo.Titulo,
                posse.Canal.Nome,
                posse.MidiaFisica))
            .OrderBy(item => item.Titulo)
            .ToListAsync(cancellationToken);

        return Ok(itens);
    }

    [HttpPost]
    public async Task<ActionResult<ItemBibliotecaResponse>> CriarItem(
        [FromBody] CriarItemBibliotecaRequest request,
        CancellationToken cancellationToken)
    {
        var titulo = request.Titulo.Trim();
        var plataforma = request.Plataforma.Trim();

        var jogo = new Jogo { Titulo = titulo };
        var canal = await _contexto.Canais
            .FirstOrDefaultAsync(item => item.Nome == plataforma, cancellationToken);

        canal ??= new Canal
        {
            Nome = plataforma,
            PermitePosse = true,
            PermitePreco = false
        };

        var posse = new PosseJogo
        {
            Jogo = jogo,
            Canal = canal,
            MidiaFisica = request.MidiaFisica,
            AdquiridoEm = DateTime.UtcNow
        };

        _contexto.PossesJogos.Add(posse);
        await _contexto.SaveChangesAsync(cancellationToken);

        var response = new ItemBibliotecaResponse(posse.Id, jogo.Titulo, canal.Nome, posse.MidiaFisica);
        return Created($"/biblioteca/{posse.Id}", response);
    }
}

public sealed record CriarItemBibliotecaRequest(
    [Required, MaxLength(200)] string Titulo,
    [Required, MaxLength(100)] string Plataforma,
    bool MidiaFisica);

public sealed record ItemBibliotecaResponse(int Id, string Titulo, string Plataforma, bool MidiaFisica);
