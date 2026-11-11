using gamelife_api.Context;
using gamelife_api.Contracts;
using gamelife_api.Services;
using Microsoft.AspNetCore.Mvc;

namespace gamelife_api.Controllers;

[ApiController]
[Route("biblioteca")]
public sealed class BibliotecaController : ControllerBase
{
    private readonly BibliotecaService _service;

    public BibliotecaController(BibliotecaService service) => _service = service;

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<JogoBibliotecaResponse>>> ObterItens(
        CancellationToken cancellationToken) =>
        Ok(await _service.ObterAsync(cancellationToken));

    public static IQueryable<ItemBibliotecaPlataformaProjection> CriarConsulta(
        GameLifeDbContext contexto) =>
        BibliotecaService.CriarConsultaPlana(contexto);

    public static string NormalizarTitulo(string? titulo) =>
        BibliotecaService.NormalizarTitulo(titulo);

    [HttpPost]
    public async Task<ActionResult<JogoBibliotecaResponse>> Salvar(
        [FromBody] SalvarJogoBibliotecaRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _service.SalvarAsync(request, cancellationToken);
            if (result.Criado)
                return Created($"/biblioteca/{result.Jogo.JogoId}", result.Jogo);
            return Ok(result.Jogo);
        }
        catch (BibliotecaValidationException exception)
        {
            return BadRequest(new ProblemDetails { Detail = exception.Message });
        }
    }
}
