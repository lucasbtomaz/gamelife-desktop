using System.ComponentModel.DataAnnotations;
using gamelife_api.Context;
using gamelife_api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace gamelife_api.Controllers;

[ApiController]
[Route("api/perfil")]
public sealed class PerfilController : ControllerBase
{
    public const int PerfilId = 1;
    public const int TamanhoMaximoFoto = 2_000_000;
    private readonly GameLifeDbContext _contexto;

    public PerfilController(GameLifeDbContext contexto) => _contexto = contexto;

    [HttpGet]
    public async Task<ActionResult<PerfilResponse>> Obter(CancellationToken cancellationToken)
        => Ok(ParaResponse(await ObterOuCriarAsync(_contexto, cancellationToken)));

    [HttpPut]
    public async Task<ActionResult<PerfilResponse>> Atualizar(
        [FromBody] AtualizarPerfilRequest request,
        CancellationToken cancellationToken)
    {
        if (request.FotoDataUrl is { Length: > TamanhoMaximoFoto } ||
            request.FotoDataUrl is not null && !request.FotoDataUrl.StartsWith("data:image/", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(new { mensagem = "A foto deve ser uma imagem de até 2 MB." });
        }

        var perfil = await ObterOuCriarAsync(_contexto, cancellationToken);
        perfil.Nome = request.Nome.Trim();
        perfil.FotoDataUrl = request.FotoDataUrl;
        perfil.ExibirJogosRecentes = request.ExibirJogosRecentes;
        await _contexto.SaveChangesAsync(cancellationToken);
        return Ok(ParaResponse(perfil));
    }

    public static async Task<PerfilLocal> ObterOuCriarAsync(GameLifeDbContext contexto, CancellationToken cancellationToken)
    {
        var perfil = await contexto.PerfisLocais.FirstOrDefaultAsync(item => item.Id == PerfilId, cancellationToken);
        if (perfil is not null) return perfil;

        perfil = new PerfilLocal();
        contexto.PerfisLocais.Add(perfil);
        try
        {
            await contexto.SaveChangesAsync(cancellationToken);
            return perfil;
        }
        catch (DbUpdateException)
        {
            contexto.Entry(perfil).State = EntityState.Detached;
            return await contexto.PerfisLocais.SingleAsync(item => item.Id == PerfilId, cancellationToken);
        }
    }

    private static PerfilResponse ParaResponse(PerfilLocal perfil)
        => new(perfil.Id, perfil.Nome, perfil.FotoDataUrl, perfil.ExibirJogosRecentes);
}

public sealed record AtualizarPerfilRequest(
    [Required, MaxLength(100)] string Nome,
    string? FotoDataUrl,
    bool ExibirJogosRecentes);

public sealed record PerfilResponse(int Id, string Nome, string? FotoDataUrl, bool ExibirJogosRecentes);
