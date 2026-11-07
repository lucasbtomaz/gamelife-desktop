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
        [FromQuery] string? search,
        CancellationToken cancellationToken)
    {
        var consulta = CriarConsulta(_contexto);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var termo = search.Trim().ToLower();
            consulta = consulta.Where(posse => posse.Titulo.ToLower().Contains(termo));
        }

        var itens = await consulta.ToListAsync(cancellationToken);
        return Ok(itens);
    }

    public static IQueryable<ItemBibliotecaResponse> CriarConsulta(GameLifeDbContext contexto)
    {
        return contexto.PossesJogos
            .AsNoTracking()
            .OrderBy(posse => posse.Jogo.Titulo)
            .Select(posse => new ItemBibliotecaResponse(
                posse.Id,
                posse.JogoId,
                posse.Jogo.Titulo,
                posse.Canal.Nome,
                posse.MidiaFisica));
    }

    public static string NormalizarTitulo(string? titulo)
    {
        if (string.IsNullOrWhiteSpace(titulo)) return string.Empty;
        var lower = titulo.Trim().ToLowerInvariant();
        var chars = lower.Select(c => char.IsLetterOrDigit(c) ? c : ' ').ToArray();
        return string.Join(" ", new string(chars).Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries));
    }

    [HttpPost]
    public async Task<ActionResult<ItemBibliotecaResponse>> CriarItem(
        [FromBody] CriarItemBibliotecaRequest request,
        CancellationToken cancellationToken)
    {
        var titulo = request.Titulo.Trim();
        var plataforma = request.Plataforma.Trim();
        var tituloNormalizado = NormalizarTitulo(titulo);

        var todosJogos = await _contexto.Jogos.ToListAsync(cancellationToken);
        var jogo = todosJogos.FirstOrDefault(item => NormalizarTitulo(item.Titulo) == tituloNormalizado)
            ?? new Jogo { Titulo = titulo };

        var plataformaNormalizada = plataforma.ToLower();
        var canal = await _contexto.Canais
            .FirstOrDefaultAsync(item => item.Nome.Trim().ToLower() == plataformaNormalizada, cancellationToken);

        canal ??= new Canal
        {
            Nome = plataforma,
            PermitePosse = true,
            PermitePreco = false
        };

        canal.PermitePosse = true;

        var posse = new PosseJogo
        {
            Jogo = jogo,
            Canal = canal,
            MidiaFisica = request.MidiaFisica,
            AdquiridoEm = DateTime.UtcNow
        };

        _contexto.PossesJogos.Add(posse);
        await _contexto.SaveChangesAsync(cancellationToken);

        var response = new ItemBibliotecaResponse(posse.Id, jogo.Id, jogo.Titulo, canal.Nome, posse.MidiaFisica);
        return Created($"/biblioteca/{posse.Id}", response);
    }
}

public sealed record CriarItemBibliotecaRequest(
    [Required, MaxLength(200)] string Titulo,
    [Required, MaxLength(100)] string Plataforma,
    bool MidiaFisica);

public sealed record ItemBibliotecaResponse(int Id, int JogoId, string Titulo, string Plataforma, bool MidiaFisica);
