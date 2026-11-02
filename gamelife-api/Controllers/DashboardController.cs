using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using gamelife_api.Context;

namespace gamelife_api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DashboardController : ControllerBase
    {
        private readonly GameLifeDbContext _contexto;

        public DashboardController(GameLifeDbContext contexto)
        {
            _contexto = contexto;
        }

        // GET: api/dashboard/stats
        [HttpGet("stats")]
        public async Task<ActionResult<object>> ObterEstatisticas()
        {
            var totalJogos = await _contexto.PossesJogos.Select(item => item.JogoId).Distinct().CountAsync();
            var totalPosses = await _contexto.PossesJogos.CountAsync();
            var totalCanais = await _contexto.Canais.CountAsync();
            var totalListaDesejos = await _contexto.ItensListaDesejos.CountAsync();

            var registros = await _contexto.RegistrosJogos.ToListAsync();
            var horasJogadas = registros.Sum(r => r.TempoEmHoras ?? 0);
            var jogosZerados = registros.Count(r => r.FinalizadoEm != null);
            var conquistasObtidas = registros.Sum(r => r.ConquistasObtidas ?? 0);
            var conquistasPossiveis = registros.Sum(r => r.ConquistasPossiveis ?? 0);
            var taxaConclusao = conquistasPossiveis > 0 ? (int)Math.Round(((double)conquistasObtidas / conquistasPossiveis) * 100) : 0;

            return Ok(new
            {
                TotalJogos = totalJogos,
                TotalPosses = totalPosses,
                TotalCanais = totalCanais,
                TotalListaDesejos = totalListaDesejos,
                HorasJogadas = Math.Round(horasJogadas, 1),
                JogosZerados = jogosZerados,
                Conquistas = new
                {
                    Obtidas = conquistasObtidas,
                    Possiveis = conquistasPossiveis,
                    TaxaConclusao = taxaConclusao
                }
            });
        }

        [HttpGet("inicio")]
        public async Task<ActionResult<object>> ObterInicio(CancellationToken cancellationToken)
        {
            var perfil = await PerfilController.ObterOuCriarAsync(_contexto, cancellationToken);
            var totalJogos = await _contexto.PossesJogos
                .Select(item => item.JogoId)
                .Distinct()
                .CountAsync(cancellationToken);
            var midiasFisicas = await _contexto.PossesJogos.CountAsync(item => item.MidiaFisica, cancellationToken);
            var jogosDigitais = await _contexto.PossesJogos.CountAsync(item => !item.MidiaFisica, cancellationToken);
            var totalColecoes = await _contexto.Colecoes.CountAsync(cancellationToken);

            var jogosRecentes = perfil.ExibirJogosRecentes
                ? await _contexto.PossesJogos.AsNoTracking()
                    .OrderByDescending(item => item.AdquiridoEm)
                    .Take(5)
                    .Select(item => new
                    {
                        item.JogoId,
                        item.Jogo.Titulo,
                        Plataforma = item.Canal.Nome,
                        item.MidiaFisica,
                        item.AdquiridoEm
                    })
                    .ToListAsync(cancellationToken)
                : [];

            var conquistas = await _contexto.Conquistas.AsNoTracking()
                .OrderByDescending(item => item.DesbloqueadaEm)
                .Take(6)
                .Select(item => new
                {
                    item.Id,
                    item.Titulo,
                    item.Descricao,
                    item.Tipo,
                    Jogo = item.Jogo != null ? item.Jogo.Titulo : null,
                    item.DesbloqueadaEm
                })
                .ToListAsync(cancellationToken);

            return Ok(new
            {
                Perfil = new { perfil.Id, perfil.Nome, perfil.FotoDataUrl, perfil.ExibirJogosRecentes },
                Resumo = new { TotalJogos = totalJogos, MidiasFisicas = midiasFisicas, JogosDigitais = jogosDigitais, TotalColecoes = totalColecoes },
                JogosRecentes = jogosRecentes,
                Conquistas = conquistas
            });
        }
    }
}
