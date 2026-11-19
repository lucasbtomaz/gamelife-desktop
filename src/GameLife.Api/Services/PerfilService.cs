using gamelife_api.Context;
using gamelife_api.Models;
using Microsoft.EntityFrameworkCore;

namespace gamelife_api.Services;

public sealed class PerfilService(GameLifeDbContext contexto)
{
    private const int PerfilId = 1;

    public async Task<PerfilLocal> ObterOuCriarAsync(CancellationToken cancellationToken)
    {
        var perfil = await contexto.PerfisLocais.FirstOrDefaultAsync(item => item.Id == PerfilId, cancellationToken);
        if (perfil is not null) return perfil;

        perfil = new PerfilLocal();
        contexto.PerfisLocais.Add(perfil);
        try
        {
            await contexto.SaveChangesAsync(cancellationToken);
            return perfil;
        }
        catch (DbUpdateException)
        {
            contexto.Entry(perfil).State = EntityState.Detached;
            return await contexto.PerfisLocais.SingleAsync(item => item.Id == PerfilId, cancellationToken);
        }
    }

    public Task SalvarAsync(CancellationToken cancellationToken) => contexto.SaveChangesAsync(cancellationToken);
}
