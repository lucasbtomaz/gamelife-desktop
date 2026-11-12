using gamelife_api.Context;
using gamelife_api.Contracts;
using gamelife_api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace gamelife_api.Controllers;

[ApiController]
[Route("api/lista-desejos")]
public sealed class ItensListaDesejosController : ControllerBase
{
    private readonly GameLifeDbContext _contexto;
    public ItensListaDesejosController(GameLifeDbContext contexto) => _contexto = contexto;

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ItemListaDesejosResponse>>> ObterItensListaDesejos()
    {
        var itens = await CarregarItens().ToListAsync();
        return Ok(itens.Select(Projetar).OrderBy(item => item.Titulo).ToArray());
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ItemListaDesejosResponse>> ObterItemPorId(int id)
    {
        var item = await CarregarItens().FirstOrDefaultAsync(item => item.Id == id);
        return item is null ? NotFound(new { mensagem = $"Item com ID {id} não encontrado." }) : Ok(Projetar(item));
    }

    [HttpPost]
    public async Task<ActionResult<ItemListaDesejosResponse>> AdicionarItem(CriarItemListaDesejosRequest request)
    {
        var titulo = request.Titulo.Trim();
        if (titulo.Length == 0) return BadRequest(new { mensagem = "Informe o nome do jogo." });
        var normalizado = titulo.ToLower();
        var jogo = await _contexto.Jogos.FirstOrDefaultAsync(item => item.Titulo.Trim().ToLower() == normalizado);
        if (jogo is not null && await _contexto.ItensListaDesejos.AnyAsync(item => item.JogoId == jogo.Id))
            return Conflict(new { mensagem = "Este jogo já está na lista de desejos." });

        jogo ??= new Jogo { Titulo = titulo };
        var desejo = new ItemListaDesejos { Jogo = jogo, PrecoDesejado = request.PrecoDesejado, Observacao = Limpar(request.Observacao), CriadoEm = DateTime.UtcNow };
        _contexto.ItensListaDesejos.Add(desejo);
        await _contexto.SaveChangesAsync();
        var criado = await CarregarItens().SingleAsync(item => item.Id == desejo.Id);
        return CreatedAtAction(nameof(ObterItemPorId), new { id = desejo.Id }, Projetar(criado));
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<ItemListaDesejosResponse>> AtualizarItem(int id, AtualizarItemListaDesejosRequest request)
    {
        var item = await _contexto.ItensListaDesejos.FindAsync(id);
        if (item is null) return NotFound(new { mensagem = $"Item com ID {id} não encontrado." });
        item.PrecoDesejado = request.PrecoDesejado;
        item.Observacao = Limpar(request.Observacao);
        await _contexto.SaveChangesAsync();
        return Ok(Projetar(await CarregarItens().SingleAsync(valor => valor.Id == id)));
    }

    [HttpPost("{id:int}/precos")]
    public async Task<ActionResult<ItemListaDesejosResponse>> AdicionarPreco(int id, AdicionarPrecoDesejoRequest request)
    {
        var desejo = await _contexto.ItensListaDesejos.FindAsync(id);
        if (desejo is null) return NotFound(new { mensagem = $"Item com ID {id} não encontrado." });
        Canal? canal = null;
        if (request.CanalId is > 0) canal = await _contexto.Canais.FindAsync(request.CanalId.Value);
        else if (!string.IsNullOrWhiteSpace(request.NomeCanal))
        {
            var nome = request.NomeCanal.Trim();
            var normalizado = nome.ToLower();
            canal = await _contexto.Canais.FirstOrDefaultAsync(item => item.Nome.Trim().ToLower() == normalizado);
            canal ??= new Canal { Nome = nome, PermitePreco = true };
        }
        if (canal is null) return BadRequest(new { mensagem = "Informe uma loja válida." });

        _contexto.RegistrosPrecos.Add(new RegistroPreco { ItemListaDesejos = desejo, Canal = canal, PrecoAtual = request.PrecoAtual, PrecoCheio = request.PrecoCheio ?? request.PrecoAtual, Disponivel = request.Disponivel, ConsultadoEm = DateTime.UtcNow });
        await _contexto.SaveChangesAsync();
        return CreatedAtAction(nameof(ObterItemPorId), new { id }, Projetar(await CarregarItens().SingleAsync(item => item.Id == id)));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> ExcluirItem(int id)
    {
        var item = await _contexto.ItensListaDesejos.Include(valor => valor.RegistrosPrecos).FirstOrDefaultAsync(valor => valor.Id == id);
        if (item is null) return NotFound(new { mensagem = $"Item com ID {id} não encontrado." });
        _contexto.ItensListaDesejos.Remove(item);
        await _contexto.SaveChangesAsync();
        return NoContent();
    }

    private IQueryable<ItemListaDesejos> CarregarItens() => _contexto.ItensListaDesejos.AsNoTracking()
        .Include(item => item.Jogo).ThenInclude(jogo => jogo.PossesJogos)
        .Include(item => item.RegistrosPrecos).ThenInclude(preco => preco.Canal);

    private static ItemListaDesejosResponse Projetar(ItemListaDesejos item)
    {
        var historico = item.RegistrosPrecos.OrderByDescending(preco => preco.ConsultadoEm).ThenByDescending(preco => preco.Id).ToArray();
        var atuais = historico.GroupBy(preco => preco.CanalId)
            .Select(grupo => grupo.OrderByDescending(preco => preco.ConsultadoEm).ThenByDescending(preco => preco.Id).First())
            .OrderBy(preco => preco.Canal.Nome)
            .Select(preco => new PrecoAtualDesejoResponse(preco.CanalId, preco.Canal.Nome, preco.PrecoAtual, preco.PrecoCheio, preco.Disponivel, preco.ConsultadoEm, historico.Where(valor => valor.CanalId == preco.CanalId && valor.Disponivel).Select(valor => (decimal?)valor.PrecoAtual).Min()))
            .ToArray();
        return new ItemListaDesejosResponse(item.Id, item.JogoId, item.Jogo.Titulo, item.Jogo.PossesJogos.Count != 0, item.PrecoDesejado, item.Observacao, item.CriadoEm, atuais, atuais.Where(preco => preco.Disponivel).Select(preco => (decimal?)preco.PrecoAtual).Min(), historico.Where(preco => preco.Disponivel).Select(preco => (decimal?)preco.PrecoAtual).Min(), historico.Select(preco => new HistoricoPrecoDesejoResponse(preco.Id, preco.CanalId, preco.Canal.Nome, preco.PrecoAtual, preco.PrecoCheio, preco.Disponivel, preco.ConsultadoEm)).ToArray());
    }

    private static string? Limpar(string? valor) => string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();
}
