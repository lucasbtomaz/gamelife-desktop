using gamelife_api.Context;
using gamelife_api.Contracts;
using gamelife_api.Models;
using Microsoft.EntityFrameworkCore;

namespace gamelife_api.Services;

public sealed class DashboardService(GameLifeDbContext contexto, PerfilService perfilService)
{
    public async Task<DashboardInicioResponse> ObterInicioAsync(CancellationToken cancellationToken)
    {
        var perfil = await perfilService.ObterOuCriarAsync(cancellationToken);
        var posses = await contexto.PossesJogos.AsNoTracking().Where(AcessoPosse.NaoCatalogoExpressao)
            .Select(item => new
            {
                item.JogoId,
                item.CompraDigital,
                item.MidiaFisica,
                Plataforma = item.Canal.Nome
            })
            .ToListAsync(cancellationToken);

        var acompanhamentos = await contexto.AcompanhamentosJogos.AsNoTracking()
            .Select(item => new
            {
                item.JogoId,
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
        var totalColecoes = await contexto.Colecoes.CountAsync(cancellationToken);

        var acompanhamentosFinalizados = acompanhamentos.Where(item => item.Status == StatusAcompanhamentoJogo.Zerado).ToList();
        var horasZeradas = acompanhamentosFinalizados.Sum(item => item.TempoEmHoras ?? 0);
        var conquistasPossiveis = acompanhamentos.Sum(item => item.ConquistasPossiveis ?? 0);
        var conquistasObtidas = acompanhamentos.Sum(item => item.ConquistasObtidas ?? 0);
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
            acompanhamentosFinalizados.Select(item => item.Plataforma));

        var maiorAnoFinalizado = acompanhamentosFinalizados
            .Where(item => item.DataStatus.HasValue)
            .Select(item => item.DataStatus!.Value.Year)
            .DefaultIfEmpty(0)
            .Max();

        var ultimoAno = Math.Max(
            2043,
            Math.Max(hoje.Year, maiorAnoFinalizado));

        var contagemPorAno = acompanhamentosFinalizados
            .Where(item => item.DataStatus.HasValue)
            .GroupBy(item => item.DataStatus!.Value.Year)
            .ToDictionary(grupo => grupo.Key, grupo => grupo.Count());

        var zeradosPorAno = Enumerable.Range(2024, ultimoAno - 2024 + 1)
            .Select(ano => new DashboardAnoResponse(ano, contagemPorAno.GetValueOrDefault(ano)))
            .ToArray();

        var jogosRecentes = perfil.ExibirJogosRecentes
            ? await contexto.PossesJogos.AsNoTracking()
                .Where(AcessoPosse.NaoCatalogoExpressao)
                .OrderByDescending(item => item.AdquiridoEm)
                .Take(5)
                .Select(item => new DashboardJogoRecenteResponse(
                    item.JogoId, item.Jogo.Titulo, item.Canal.Nome, item.CompraDigital,
                    item.MidiaFisica, item.PorAssinatura, item.AdquiridoEm))
                .ToListAsync(cancellationToken)
            : [];

        var conquistas = await contexto.Conquistas.AsNoTracking()
            .OrderByDescending(item => item.DesbloqueadaEm)
            .Take(6)
            .Select(item => new DashboardConquistaResponse(
                item.Id, item.Titulo, item.Descricao, item.Tipo,
                item.Jogo != null ? item.Jogo.Titulo : null, item.DesbloqueadaEm))
            .ToListAsync(cancellationToken);

        var alertasListaDesejos = await ObterAlertasDesejosAsync(cancellationToken);
        var jogosNaBiblioteca = posses.Select(item => item.JogoId).ToHashSet();
        var jogosEncerrados = acompanhamentos
            .Where(item => item.Status is StatusAcompanhamentoJogo.Zerado or StatusAcompanhamentoJogo.Dropado)
            .Select(item => item.JogoId)
            .ToHashSet();
        var alertasSaidaCatalogo = await ObterSaidasCatalogoAsync(
            hoje, jogosNaBiblioteca, jogosEncerrados, cancellationToken);

        return new DashboardInicioResponse(
            new PerfilResponse(perfil.Id, perfil.Nome, perfil.FotoDataUrl, perfil.ExibirJogosRecentes, perfil.DataEntrada),
            new DashboardResumoResponse(totalJogos, midiasFisicas, jogosDigitais, totalColecoes,
                tempoNaPlataformaAnos, acompanhamentosFinalizados.Count, horasZeradas,
                conquistasPossiveis, conquistasObtidas, percentualConquistas),
            bibliotecaPorPlataforma, zeradosPorPlataforma, zeradosPorAno, jogosRecentes,
            conquistas, alertasListaDesejos, alertasSaidaCatalogo);
    }

    private async Task<DashboardAlertaDesejoResponse[]> ObterAlertasDesejosAsync(CancellationToken cancellationToken)
    {
        var alertasDesejosBrutos = await contexto.ItensListaDesejos.AsNoTracking()
            .Include(item => item.Jogo)
            .Include(item => item.RegistrosPrecos).ThenInclude(preco => preco.Canal)
            .Where(item => !item.AlertaBibliotecaSilenciado &&
                           item.Jogo.PossesJogos.Any(posse => !posse.PorAssinatura ||
                               posse.CompraDigital || posse.MidiaFisica || posse.ResgatadoDeAssinatura) &&
                           item.RegistrosPrecos.Any())
            .ToListAsync(cancellationToken);

        return alertasDesejosBrutos
            .OrderBy(item => item.Jogo.Titulo)
            .Select(item => new DashboardAlertaDesejoResponse(item.Id, item.JogoId, item.Jogo.Titulo,
                item.RegistrosPrecos
                    .Select(preco => preco.Canal.Nome)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(nome => nome, StringComparer.CurrentCultureIgnoreCase)
                    .ToArray(),
                item.RegistrosPrecos
                    .GroupBy(preco => preco.CanalId)
                    .Select(grupo => grupo.OrderByDescending(preco => preco.ConsultadoEm).ThenByDescending(preco => preco.Id).First())
                    .Where(preco => preco.Disponivel)
                    .Select(preco => (decimal?)preco.PrecoAtual)
                    .Min()))
            .ToArray();
    }

    private async Task<DashboardSaidaCatalogoResponse[]> ObterSaidasCatalogoAsync(
        DateOnly hoje, IReadOnlySet<int> jogosNaBiblioteca, IReadOnlySet<int> jogosEncerrados,
        CancellationToken cancellationToken)
    {
        var inicio = hoje.ToDateTime(TimeOnly.MinValue);
        var limiteExclusivo = inicio.AddDays(31);
        var saidas = await contexto.AssinaturasJogos.AsNoTracking()
            .Where(item => item.Situacao == "Disponível" &&
                           item.DataEncerramento >= inicio && item.DataEncerramento < limiteExclusivo)
            .Select(item => new { item.JogoId, item.Jogo.Titulo, item.Servico, item.DataEncerramento })
            .ToListAsync(cancellationToken);
        return saidas
            .Where(item => !jogosNaBiblioteca.Contains(item.JogoId) && !jogosEncerrados.Contains(item.JogoId))
            .Select(item => new DashboardSaidaCatalogoResponse(
                item.JogoId, item.Titulo, item.Servico, item.DataEncerramento!.Value,
                (item.DataEncerramento.Value.Date - inicio).Days))
            .OrderBy(item => item.DiasRestantes)
            .ThenBy(item => item.Titulo)
            .ToArray();
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
