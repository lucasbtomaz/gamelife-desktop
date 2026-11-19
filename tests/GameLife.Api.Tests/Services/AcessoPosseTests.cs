using gamelife_api.Models;
using gamelife_api.Services;

namespace gamelife_api.Tests.Services;

public sealed class AcessoPosseTests
{
    [Theory]
    [InlineData(true, false, true, true, null, "", EstadoAcesso.Possuido)]
    [InlineData(false, true, false, false, null, "", EstadoAcesso.Possuido)]
    [InlineData(false, false, false, false, null, "", EstadoAcesso.Possuido)]
    [InlineData(false, false, true, true, "playstation plus", "", EstadoAcesso.JogavelPorResgate)]
    [InlineData(false, false, true, true, "Game Pass", "", EstadoAcesso.BloqueadoSemAssinatura)]
    [InlineData(false, false, true, true, null, "", EstadoAcesso.BloqueadoSemAssinatura)]
    [InlineData(false, false, true, false, null, "", EstadoAcesso.Catalogo)]
    [InlineData(false, false, true, false, null, "Encerrado", EstadoAcesso.SaiuDoCatalogo)]
    [InlineData(false, false, true, false, null, "Encerrado,Disponível", EstadoAcesso.Catalogo)]
    public void ClassificaAcesso(bool compra, bool midia, bool assinatura, bool resgate,
        string? servico, string situacoes, EstadoAcesso esperado)
    {
        var ativos = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "PlayStation Plus" };
        Assert.Equal(esperado, AcessoPosse.Classificar(compra, midia, assinatura, resgate, servico,
            ativos, situacoes.Split(',', StringSplitOptions.RemoveEmptyEntries)));
    }

    [Fact]
    public void ExpressoesConcordamComARegraParaTodasAsFlags()
    {
        var catalogo = AcessoPosse.EhCatalogoExpressao.Compile();
        var biblioteca = AcessoPosse.NaoCatalogoExpressao.Compile();
        for (var flags = 0; flags < 16; flags++)
        {
            var posse = new PosseJogo
            {
                CompraDigital = (flags & 1) != 0,
                MidiaFisica = (flags & 2) != 0,
                PorAssinatura = (flags & 4) != 0,
                ResgatadoDeAssinatura = (flags & 8) != 0
            };
            Assert.Equal(flags == 4, AcessoPosse.EhCatalogo(
                posse.CompraDigital, posse.MidiaFisica, posse.PorAssinatura, posse.ResgatadoDeAssinatura));
            Assert.Equal(flags == 4, catalogo(posse));
            Assert.Equal(flags != 4, biblioteca(posse));
        }
    }
}
