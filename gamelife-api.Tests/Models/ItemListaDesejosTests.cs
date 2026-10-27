using gamelife_api.Models;

namespace gamelife_api.Tests.Models;

public sealed class ItemListaDesejosTests
{
    [Fact]
    public void ExponheHistoricoDePrecosDoItem()
    {
        var propriedade = typeof(ItemListaDesejos).GetProperty("RegistrosPrecos");

        Assert.NotNull(propriedade);
        Assert.Equal(typeof(ICollection<RegistroPreco>), propriedade.PropertyType);
        Assert.Null(typeof(ItemListaDesejos).GetProperty("Registros"));
    }
}
