using gamelife_api.Context;
using gamelife_api.Models;
using gamelife_api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace gamelife_api.Controllers
{
    [ApiController]
    [Route("api/dashboard")]
    public sealed class DashboardController : ControllerBase
    {
        private readonly GameLifeDbContext _contexto;

        public DashboardController(GameLifeDbContext contexto) => _contexto = contexto;

        [HttpGet("inicio")]
        public async Task<ActionResult<object>> ObterInicio(CancellationToken cancellationToken)
        {
            var perfil = await PerfilController.ObterOuCriarAsync(_contexto, cancellationToken);
            var posses = await _contexto.PossesJogos.AsNoTracking()
                .Select(item => new
                {
                    item.JogoId,
                    item.CompraDigital,
                    item.MidiaFisica,
                    item.PorAssinatura,
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
            var jogosDigitais = posses.Count(item => item.CompraDigital);
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
                        item.CompraDigital,
                        item.MidiaFisica,
                        item.PorAssinatura,
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

            var alertasDesejosBrutos = await _contexto.ItensListaDesejos.AsNoTracking()
                .Include(item => item.Jogo).ThenInclude(jogo => jogo.PossesJogos)
                .Include(item => item.RegistrosPrecos).ThenInclude(preco => preco.Canal)
                .Where(item => !item.AlertaBibliotecaSilenciado &&
                               item.Jogo.PossesJogos.Any() &&
                               item.RegistrosPrecos.Any())
                .ToListAsync(cancellationToken);

            var alertasListaDesejos = alertasDesejosBrutos
                .OrderBy(item => item.Jogo.Titulo)
                .Select(item => new
                {
                    DesejoId = item.Id,
                    item.JogoId,
                    Titulo = item.Jogo.Titulo,
                    LojasMonitoradas = item.RegistrosPrecos
                        .Select(preco => preco.Canal.Nome)
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .OrderBy(nome => nome, StringComparer.CurrentCultureIgnoreCase)
                        .ToArray(),
                    MenorPrecoAtual = item.RegistrosPrecos
                        .GroupBy(preco => preco.CanalId)
                        .Select(grupo => grupo.OrderByDescending(preco => preco.ConsultadoEm).ThenByDescending(preco => preco.Id).First())
                        .Where(preco => preco.Disponivel)
                        .Select(preco => (decimal?)preco.PrecoAtual)
                        .Min()
                })
                .ToArray();

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
                Conquistas = conquistas,
                AlertasListaDesejos = alertasListaDesejos
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
