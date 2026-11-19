using System.Linq.Expressions;
using gamelife_api.Models;

namespace gamelife_api.Services;

public enum EstadoAcesso
{
    Possuido,
    JogavelPorResgate,
    BloqueadoSemAssinatura,
    Catalogo,
    SaiuDoCatalogo
}

public static class AcessoPosse
{
    public static readonly Expression<Func<PosseJogo, bool>> EhCatalogoExpressao = posse =>
        posse.PorAssinatura && !posse.CompraDigital && !posse.MidiaFisica && !posse.ResgatadoDeAssinatura;

    public static readonly Expression<Func<PosseJogo, bool>> NaoCatalogoExpressao = posse =>
        !posse.PorAssinatura || posse.CompraDigital || posse.MidiaFisica || posse.ResgatadoDeAssinatura;

    public static bool EhCatalogo(bool compraDigital, bool midiaFisica, bool porAssinatura, bool resgatado) =>
        porAssinatura && !compraDigital && !midiaFisica && !resgatado;

    public static EstadoAcesso Classificar(
        bool compraDigital, bool midiaFisica, bool porAssinatura, bool resgatado,
        string? servicoExigido,
        IReadOnlySet<string> servicosAtivos,
        IReadOnlyCollection<string> situacoesDasAssinaturasDoJogo)
    {
        if (compraDigital || midiaFisica)
            return EstadoAcesso.Possuido;

        if (resgatado)
            return servicoExigido is not null && servicosAtivos.Contains(servicoExigido)
                ? EstadoAcesso.JogavelPorResgate
                : EstadoAcesso.BloqueadoSemAssinatura;

        if (!porAssinatura)
            return EstadoAcesso.Possuido;

        return situacoesDasAssinaturasDoJogo.Count > 0 &&
               situacoesDasAssinaturasDoJogo.All(item => item == "Encerrado")
            ? EstadoAcesso.SaiuDoCatalogo
            : EstadoAcesso.Catalogo;
    }
}
