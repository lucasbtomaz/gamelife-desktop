using gamelife_api.Context;
using gamelife_api.Contracts;
using gamelife_api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace gamelife_api.Controllers;

[ApiController]
[Route("api/minhas-assinaturas")]
public sealed class MinhasAssinaturasController(GameLifeDbContext contexto) : ControllerBase
{
    private static readonly SemaphoreSlim EscritaGate = new(1, 1);

    [HttpGet]
    public async Task<IReadOnlyList<MinhaAssinaturaResponse>> Obter(CancellationToken cancellationToken)
    {
        var salvas = await contexto.MinhasAssinaturas.AsNoTracking()
            .ToDictionaryAsync(item => item.Servico, StringComparer.OrdinalIgnoreCase, cancellationToken);

        return AssinaturaCatalogo.ServicosSuportados
            .OrderBy(servico => servico, StringComparer.OrdinalIgnoreCase)
            .Select(servico => salvas.TryGetValue(servico, out var item)
                ? Projetar(item)
                : new MinhaAssinaturaResponse(servico, false, null))
            .ToArray();
    }

    [HttpPut]
    public async Task<ActionResult<MinhaAssinaturaResponse>> Salvar(
        SalvarMinhaAssinaturaRequest request, CancellationToken cancellationToken)
    {
        string servico;
        try
        {
            servico = AssinaturaCatalogo.NormalizarServico(request.Servico);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { mensagem = ex.Message });
        }

        await EscritaGate.WaitAsync(cancellationToken);
        try
        {
            var item = await contexto.MinhasAssinaturas
                .SingleOrDefaultAsync(item => item.Servico == servico, cancellationToken);
            if (item is null)
            {
                item = new MinhaAssinatura { Servico = servico };
                contexto.MinhasAssinaturas.Add(item);
            }

            item.Ativa = request.Ativa;
            item.ProximaRenovacao = request.ProximaRenovacao;
            await contexto.SaveChangesAsync(cancellationToken);
            return Ok(Projetar(item));
        }
        finally
        {
            EscritaGate.Release();
        }
    }

    private static MinhaAssinaturaResponse Projetar(MinhaAssinatura item) =>
        new(item.Servico, item.Ativa, item.ProximaRenovacao);
}
