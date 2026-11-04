using gamelife_api.Context;
using Microsoft.EntityFrameworkCore;

namespace GameLife.Desktop;

public static class LegacyDatabaseMigration
{
    public static async Task CopiarAsync(
        GameLifeDbContext source,
        GameLifeDbContext destination,
        CancellationToken cancellationToken = default)
    {
        if (await destination.Jogos.AnyAsync(cancellationToken) ||
            await destination.Canais.AnyAsync(cancellationToken))
            return;

        var canais = await source.Canais.AsNoTracking().ToListAsync(cancellationToken);
        var jogos = await source.Jogos.AsNoTracking().ToListAsync(cancellationToken);
        var perfis = await source.PerfisLocais.AsNoTracking().ToListAsync(cancellationToken);
        var colecoes = await source.Colecoes.AsNoTracking().ToListAsync(cancellationToken);
        var posses = await source.PossesJogos.AsNoTracking().ToListAsync(cancellationToken);
        var desejos = await source.ItensListaDesejos.AsNoTracking().ToListAsync(cancellationToken);
        var colecoesJogos = await source.ColecoesJogos.AsNoTracking().ToListAsync(cancellationToken);
        var conquistas = await source.Conquistas.AsNoTracking().ToListAsync(cancellationToken);
        var motivos = await source.MotivosDesejos.AsNoTracking().ToListAsync(cancellationToken);
        var precos = await source.RegistrosPrecos.AsNoTracking().ToListAsync(cancellationToken);
        var registros = await source.RegistrosJogos.AsNoTracking().ToListAsync(cancellationToken);

        await using var transaction = await destination.Database.BeginTransactionAsync(cancellationToken);
        destination.AddRange(canais);
        destination.AddRange(jogos);
        destination.AddRange(perfis);
        destination.AddRange(colecoes);
        await destination.SaveChangesAsync(cancellationToken);

        destination.AddRange(posses);
        destination.AddRange(desejos);
        await destination.SaveChangesAsync(cancellationToken);

        destination.AddRange(colecoesJogos);
        destination.AddRange(conquistas);
        destination.AddRange(motivos);
        destination.AddRange(precos);
        destination.AddRange(registros);
        await destination.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }
}
