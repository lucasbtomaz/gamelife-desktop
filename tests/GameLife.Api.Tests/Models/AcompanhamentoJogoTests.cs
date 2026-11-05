using System.ComponentModel.DataAnnotations;
using gamelife_api.Context;
using gamelife_api.Models;
using Microsoft.EntityFrameworkCore;

namespace gamelife_api.Tests.Models;

public sealed class AcompanhamentoJogoTests
{
    [Fact]
    public void OfereceOsTresStatusDeAcompanhamento()
    {
        Assert.Equal(
            ["NaoTerminado", "Zerado", "Dropado"],
            Enum.GetNames<StatusAcompanhamentoJogo>());
    }

    [Fact]
    public void RejeitaTempoEConquistasNegativos()
    {
        var acompanhamento = new AcompanhamentoJogo
        {
            JogoId = 1,
            CanalId = 1,
            TempoEmHoras = -1,
            ConquistasPossiveis = -1,
            ConquistasObtidas = -1
        };
        var resultados = new List<ValidationResult>();

        Validator.TryValidateObject(acompanhamento, new ValidationContext(acompanhamento), resultados, true);

        Assert.Equal(3, resultados.Count);
    }

    [Fact]
    public void ConfiguraPosseOpcionalEIndiceUnicoPorJogoECanal()
    {
        var options = new DbContextOptionsBuilder<GameLifeDbContext>()
            .UseInMemoryDatabase($"modelo-{Guid.NewGuid()}")
            .Options;
        using var contexto = new GameLifeDbContext(options);
        var entidade = contexto.Model.FindEntityType(typeof(AcompanhamentoJogo));

        Assert.NotNull(entidade);
        Assert.True(entidade.FindProperty(nameof(AcompanhamentoJogo.PosseJogoId))!.IsNullable);
        var indice = Assert.Single(entidade.GetIndexes(), item => item.IsUnique);
        Assert.Equal(
            [nameof(AcompanhamentoJogo.JogoId), nameof(AcompanhamentoJogo.CanalId)],
            indice.Properties.Select(item => item.Name));
    }
}
