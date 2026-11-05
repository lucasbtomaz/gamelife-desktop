using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using gamelife_api.Context;
using gamelife_api.Services;
using gamelife_api.Models;

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

            var registros = await _contexto.AcompanhamentosJogos.ToListAsync();
            var horasJogadas = registros.Sum(r => r.TempoEmHoras ?? 0);
            var jogosZerados = registros.Count(r => r.Status == StatusAcompanhamentoJogo.Zerado);
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
            var posses = await _contexto.PossesJogos.AsNoTracking()
                .Select(item => new
                {
                    item.JogoId,
                    item.MidiaFisica,
                    Plataforma = item.Canal.Nome
                })
                .ToListAsync(cancellationToken);
            var registros = await _contexto.AcompanhamentosJogos.AsNoTracking()
                .Select(item => new
                {
                    item.Status,
                    item.DataStatus,
                    item.TempoEmHoras,
                    item.ConquistasPossiveis,
                    item.ConquistasObtidas,
                    Plataforma = item.Canal.Nome
                })
                .ToListAsync(cancellationToken);

            var totalJogos = posses.Select(item => item.JogoId).Distinct().Count();
            var midiasFisicas = posses.Count(item => item.MidiaFisica);
            var jogosDigitais = posses.Count(item => !item.MidiaFisica);
            var totalColecoes = await _contexto.Colecoes.CountAsync(cancellationToken);
            var registrosFinalizados = registros.Where(item => item.Status == StatusAcompanhamentoJogo.Zerado).ToList();
            var horasZeradas = registrosFinalizados.Sum(item => item.TempoEmHoras ?? 0);
            var conquistasPossiveis = registros.Sum(item => item.ConquistasPossiveis ?? 0);
            var conquistasObtidas = registros.Sum(item => item.ConquistasObtidas ?? 0);
            var percentualConquistas = conquistasPossiveis == 0
                ? 0
                : Math.Round(conquistasObtidas * 100m / conquistasPossiveis, 2, MidpointRounding.AwayFromZero);
            var hoje = DateOnly.FromDateTime(DateTime.Today);
            var tempoNaPlataformaAnos = DashboardResumoCalculator.CalcularAnosCompletos(
                DateOnly.FromDateTime(perfil.DataEntrada),
                hoje);

            var bibliotecaPorPlataforma = CriarDistribuicaoPlataformas(
                posses.Select(item => item.Plataforma));
            var zeradosPorPlataforma = CriarDistribuicaoPlataformas(
                registrosFinalizados.Select(item => item.Plataforma));
            var maiorAnoFinalizado = registrosFinalizados
                .Where(item => item.DataStatus.HasValue)
                .Select(item => item.DataStatus!.Value.Year)
                .DefaultIfEmpty(0)
                .Max();
            var ultimoAno = Math.Max(
                2043,
                Math.Max(hoje.Year, maiorAnoFinalizado));
            var contagemPorAno = registrosFinalizados
                .Where(item => item.DataStatus.HasValue)
                .GroupBy(item => item.DataStatus!.Value.Year)
                .ToDictionary(grupo => grupo.Key, grupo => grupo.Count());
            var zeradosPorAno = Enumerable.Range(2024, ultimoAno - 2024 + 1)
                .Select(ano => new DashboardAnoResponse(ano, contagemPorAno.GetValueOrDefault(ano)))
                .ToArray();

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
                Perfil = new { perfil.Id, perfil.Nome, perfil.FotoDataUrl, perfil.ExibirJogosRecentes, perfil.DataEntrada },
                Resumo = new
                {
                    TotalJogos = totalJogos,
                    MidiasFisicas = midiasFisicas,
                    JogosDigitais = jogosDigitais,
                    TotalColecoes = totalColecoes,
                    TempoNaPlataformaAnos = tempoNaPlataformaAnos,
                    JogosZerados = registrosFinalizados.Count,
                    HorasZeradas = horasZeradas,
                    ConquistasPossiveis = conquistasPossiveis,
                    ConquistasObtidas = conquistasObtidas,
                    PercentualConquistas = percentualConquistas
                },
                BibliotecaPorPlataforma = bibliotecaPorPlataforma,
                ZeradosPorPlataforma = zeradosPorPlataforma,
                ZeradosPorAno = zeradosPorAno,
                JogosRecentes = jogosRecentes,
                Conquistas = conquistas
            });
        }

        private static DashboardDistribuicaoResponse[] CriarDistribuicaoPlataformas(IEnumerable<string> plataformas)
        {
            var contagens = plataformas
                .Select(DashboardResumoCalculator.NormalizarPlataforma)
                .GroupBy(nome => nome, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(grupo => grupo.Key, grupo => grupo.Count(), StringComparer.OrdinalIgnoreCase);
            var extras = contagens.Keys
                .Except(DashboardResumoCalculator.PlataformasPrincipais, StringComparer.OrdinalIgnoreCase)
                .OrderBy(nome => nome, StringComparer.CurrentCultureIgnoreCase);

            return DashboardResumoCalculator.PlataformasPrincipais
                .Concat(extras)
                .Select(nome => new DashboardDistribuicaoResponse(nome, contagens.GetValueOrDefault(nome)))
                .ToArray();
        }
    }
}

public sealed record DashboardDistribuicaoResponse(string Plataforma, int Total);
public sealed record DashboardAnoResponse(int Ano, int Total);
