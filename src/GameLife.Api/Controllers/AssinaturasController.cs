using gamelife_api.Context;
using gamelife_api.Contracts;
using gamelife_api.Models;
using gamelife_api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace gamelife_api.Controllers;

[ApiController]
[Route("api/assinaturas")]
public sealed class AssinaturasController : ControllerBase
{
    private readonly GameLifeDbContext _contexto;

    public AssinaturasController(GameLifeDbContext contexto) => _contexto = contexto;

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<AssinaturaJogoResponse>>> Obter(
        [FromQuery] int? jogoId,
        [FromQuery] string? titulo,
        CancellationToken cancellationToken)
    {
        if (jogoId.HasValue)
        {
            var itens = await _contexto.AssinaturasJogos.AsNoTracking()
                .Where(item => item.JogoId == jogoId.Value)
                .OrderBy(item => item.Servico)
                .ToListAsync(cancellationToken);
            return Ok(itens.Select(BibliotecaService.ProjetarAssinatura).ToArray());
        }

        if (!string.IsNullOrWhiteSpace(titulo))
        {
            var chave = BibliotecaService.NormalizarTitulo(titulo);
            var jogos = await _contexto.Jogos.AsNoTracking().ToListAsync(cancellationToken);
            var jogo = jogos.FirstOrDefault(item => BibliotecaService.NormalizarTitulo(item.Titulo) == chave);
            if (jogo is null) return Ok(Array.Empty<AssinaturaJogoResponse>());

            var itens = await _contexto.AssinaturasJogos.AsNoTracking()
                .Where(item => item.JogoId == jogo.Id)
                .OrderBy(item => item.Servico)
                .ToListAsync(cancellationToken);
            return Ok(itens.Select(BibliotecaService.ProjetarAssinatura).ToArray());
        }

        var todas = await _contexto.AssinaturasJogos.AsNoTracking()
            .OrderBy(item => item.JogoId)
            .ThenBy(item => item.Servico)
            .ToListAsync(cancellationToken);
        return Ok(todas.Select(BibliotecaService.ProjetarAssinatura).ToArray());
    }

    [HttpPost]
    [HttpPut]
    public async Task<ActionResult<AssinaturaJogoResponse>> Salvar(
        [FromBody] SalvarAssinaturaJogoRequest request,
        CancellationToken cancellationToken)
    {
        string servico;
        string situacao;
        try
        {
            servico = AssinaturaCatalogo.NormalizarServico(request.Servico);
            situacao = AssinaturaCatalogo.NormalizarSituacao(request.Situacao);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { mensagem = ex.Message });
        }

        if (request.DataDisponibilidade.HasValue &&
            request.DataEncerramento.HasValue &&
            request.DataEncerramento.Value.Date < request.DataDisponibilidade.Value.Date)
        {
            return BadRequest(new { mensagem = "A data de encerramento não pode ser anterior à data de disponibilidade." });
        }

        Jogo? jogo = null;
        if (request.JogoId is > 0)
        {
            jogo = await _contexto.Jogos.FindAsync([request.JogoId.Value], cancellationToken);
            if (jogo is null)
                return NotFound(new { mensagem = $"Jogo com ID {request.JogoId.Value} não encontrado." });
        }
        else if (!string.IsNullOrWhiteSpace(request.Titulo))
        {
            var titulo = request.Titulo.Trim();
            var chave = BibliotecaService.NormalizarTitulo(titulo);
            var jogos = await _contexto.Jogos.ToListAsync(cancellationToken);
            jogo = jogos.FirstOrDefault(item => BibliotecaService.NormalizarTitulo(item.Titulo) == chave);
            if (jogo is null)
            {
                jogo = new Jogo { Titulo = titulo };
                _contexto.Jogos.Add(jogo);
            }
        }
        else
        {
            return BadRequest(new { mensagem = "Informe o jogo para salvar a assinatura." });
        }

        var assinatura = jogo.Id == 0
            ? null
            : await _contexto.AssinaturasJogos.FirstOrDefaultAsync(
                item => item.JogoId == jogo.Id && item.Servico == servico,
                cancellationToken);

        var criada = assinatura is null;
        if (assinatura is null)
        {
            assinatura = new AssinaturaJogo
            {
                Jogo = jogo,
                Servico = servico,
                Situacao = situacao,
                DataDisponibilidade = request.DataDisponibilidade,
                DataEncerramento = request.DataEncerramento
            };
            _contexto.AssinaturasJogos.Add(assinatura);
        }
        else
        {
            assinatura.Situacao = situacao;
            assinatura.DataDisponibilidade = request.DataDisponibilidade;
            assinatura.DataEncerramento = request.DataEncerramento;
        }

        await _contexto.SaveChangesAsync(cancellationToken);
        var resposta = BibliotecaService.ProjetarAssinatura(assinatura);
        return criada
            ? Created($"/api/assinaturas?jogoId={assinatura.JogoId}", resposta)
            : Ok(resposta);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Excluir(int id, CancellationToken cancellationToken)
    {
        var assinatura = await _contexto.AssinaturasJogos.FindAsync([id], cancellationToken);
        if (assinatura is null)
            return NotFound(new { mensagem = $"Assinatura com ID {id} não encontrada." });

        _contexto.AssinaturasJogos.Remove(assinatura);
        await _contexto.SaveChangesAsync(cancellationToken);
        return NoContent();
    }
}
