using gamelife_api.Services;

namespace gamelife_api.Tests.Services;

public sealed class DashboardResumoCalculatorTests
{
    [Theory]
    [InlineData(2026, 11, 4, 6)]
    [InlineData(2026, 9, 28, 5)]
    [InlineData(2019, 9, 29, 0)]
    public void CalculaSomenteAnosCompletos(int ano, int mes, int dia, int esperado)
    {
        var inicio = new DateOnly(2020, 9, 29);
        var hoje = new DateOnly(ano, mes, dia);

        var resultado = DashboardResumoCalculator.CalcularAnosCompletos(inicio, hoje);

        Assert.Equal(esperado, resultado);
    }

    [Theory]
    [InlineData("PlayStation 5", "PSN")]
    [InlineData("Xbox Series X|S", "XBOX")]
    [InlineData("Nintendo Switch", "NINTENDO")]
    [InlineData("Épic", "EPIC")]
    [InlineData("níntendo switch", "NINTENDO")]
    [InlineData("  Plataforma independente  ", "Plataforma independente")]
    public void NormalizaAliasesSemDescartarPlataformasDesconhecidas(string nome, string esperado)
    {
        var resultado = DashboardResumoCalculator.NormalizarPlataforma(nome);

        Assert.Equal(esperado, resultado);
    }
}
