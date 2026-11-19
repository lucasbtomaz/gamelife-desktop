using gamelife_api.Contracts;
using gamelife_api.Services;
using gamelife_api.Models;
using Microsoft.AspNetCore.Mvc;

namespace gamelife_api.Controllers;

[ApiController]
[Route("api/perfil")]
public sealed class PerfilController(PerfilService service) : ControllerBase
{
    public const int TamanhoMaximoFoto = 2_000_000;
    [HttpGet]
    public async Task<ActionResult<PerfilResponse>> Obter(CancellationToken cancellationToken)
        => Ok(ParaResponse(await service.ObterOuCriarAsync(cancellationToken)));

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

        var perfil = await service.ObterOuCriarAsync(cancellationToken);
        perfil.Nome = request.Nome.Trim();
        perfil.FotoDataUrl = request.FotoDataUrl;
        perfil.ExibirJogosRecentes = request.ExibirJogosRecentes;
        await service.SalvarAsync(cancellationToken);
        return Ok(ParaResponse(perfil));
    }

    private static PerfilResponse ParaResponse(PerfilLocal perfil)
        => new(perfil.Id, perfil.Nome, perfil.FotoDataUrl, perfil.ExibirJogosRecentes, perfil.DataEntrada);
}
