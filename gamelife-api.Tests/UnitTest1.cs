namespace gamelife_api.Tests;

public sealed class ProgramTypeTests
{
    [Fact]
    public void ProgramEhPublicoParaTestesDeIntegracao()
    {
        Assert.True(typeof(Program).IsPublic);
    }
}
