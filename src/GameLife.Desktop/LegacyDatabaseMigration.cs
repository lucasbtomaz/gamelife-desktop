using gamelife_api.Context;
using gamelife_api.Models;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace GameLife.Desktop;

public static class LegacyDatabaseMigration
{
    public static async Task MesclarAsync(
        GameLifeDbContext source,
        GameLifeDbContext destination,
        CancellationToken cancellationToken = default)
    {
        await using var transaction = await destination.Database.BeginTransactionAsync(cancellationToken);
        var sourceChannels = await source.Canais.AsNoTracking().ToListAsync(cancellationToken);
        var destinationChannels = await destination.Canais.ToListAsync(cancellationToken);
        var channelMap = new Dictionary<int, Canal>();
        foreach (var sourceChannel in sourceChannels)
        {
            var channel = destinationChannels.FirstOrDefault(item =>
                item.Nome.Equals(sourceChannel.Nome, StringComparison.OrdinalIgnoreCase));
            if (channel is null)
            {
                channel = new Canal
                {
                    Nome = sourceChannel.Nome,
                    PermitePosse = sourceChannel.PermitePosse,
                    PermitePreco = sourceChannel.PermitePreco
                };
                destination.Canais.Add(channel);
                destinationChannels.Add(channel);
            }
            channelMap[sourceChannel.Id] = channel;
        }

        var sourceGames = await source.Jogos.AsNoTracking().ToListAsync(cancellationToken);
        var destinationGames = await destination.Jogos.ToListAsync(cancellationToken);
        var gameMap = new Dictionary<int, Jogo>();
        foreach (var sourceGame in sourceGames)
        {
            var game = destinationGames.FirstOrDefault(item =>
                item.Titulo.Equals(sourceGame.Titulo, StringComparison.OrdinalIgnoreCase));
            if (game is null)
            {
                game = new Jogo
                {
                    Titulo = sourceGame.Titulo,
                    AnoLancamento = sourceGame.AnoLancamento
                };
                destination.Jogos.Add(game);
                destinationGames.Add(game);
            }
            gameMap[sourceGame.Id] = game;
        }
        await destination.SaveChangesAsync(cancellationToken);

        var destinationOwnerships = await destination.PossesJogos.ToListAsync(cancellationToken);
        var ownershipMap = new Dictionary<int, PosseJogo>();
        foreach (var sourceOwnership in await source.PossesJogos.AsNoTracking().ToListAsync(cancellationToken))
        {
            var game = gameMap[sourceOwnership.JogoId];
            var channel = channelMap[sourceOwnership.CanalId];
            var ownership = destinationOwnerships.FirstOrDefault(item =>
                item.JogoId == game.Id && item.CanalId == channel.Id);
            if (ownership is null)
            {
                ownership = new PosseJogo
                {
                    JogoId = game.Id,
                    CanalId = channel.Id,
                    MidiaFisica = sourceOwnership.MidiaFisica,
                    AdquiridoEm = sourceOwnership.AdquiridoEm
                };
                destination.PossesJogos.Add(ownership);
                destinationOwnerships.Add(ownership);
            }
            ownershipMap[sourceOwnership.Id] = ownership;
        }
        await destination.SaveChangesAsync(cancellationToken);

        var destinationRecords = await destination.RegistrosJogos.ToListAsync(cancellationToken);
        foreach (var sourceRecord in await source.RegistrosJogos.AsNoTracking().ToListAsync(cancellationToken))
        {
            if (!ownershipMap.TryGetValue(sourceRecord.PosseJogoId, out var ownership))
                continue;
            var exists = destinationRecords.Any(item =>
                item.PosseJogoId == ownership.Id &&
                item.FinalizadoEm == sourceRecord.FinalizadoEm &&
                item.TempoEmHoras == sourceRecord.TempoEmHoras);
            if (!exists)
            {
                destination.RegistrosJogos.Add(new RegistroJogo
                {
                    PosseJogoId = ownership.Id,
                    FinalizadoEm = sourceRecord.FinalizadoEm,
                    TempoEmHoras = sourceRecord.TempoEmHoras,
                    Dificuldade = sourceRecord.Dificuldade,
                    ConquistasPossiveis = sourceRecord.ConquistasPossiveis,
                    ConquistasObtidas = sourceRecord.ConquistasObtidas
                });
            }
        }

        if (await TabelaExisteAsync(source, "AcompanhamentosJogos", cancellationToken))
        {
            var destinationTrackings = await destination.AcompanhamentosJogos.ToListAsync(cancellationToken);
            foreach (var sourceTracking in await source.AcompanhamentosJogos.AsNoTracking().ToListAsync(cancellationToken))
            {
                var game = gameMap[sourceTracking.JogoId];
                var channel = channelMap[sourceTracking.CanalId];
                if (destinationTrackings.Any(item =>
                    item.JogoId == game.Id && item.CanalId == channel.Id))
                {
                    continue;
                }

                ownershipMap.TryGetValue(sourceTracking.PosseJogoId ?? 0, out var ownership);
                var tracking = new AcompanhamentoJogo
                {
                    JogoId = game.Id,
                    CanalId = channel.Id,
                    PosseJogoId = ownership?.Id,
                    Status = sourceTracking.Status,
                    DataStatus = sourceTracking.DataStatus,
                    TempoEmHoras = sourceTracking.TempoEmHoras,
                    ConquistasPossiveis = sourceTracking.ConquistasPossiveis,
                    ConquistasObtidas = sourceTracking.ConquistasObtidas
                };
                destination.AcompanhamentosJogos.Add(tracking);
                destinationTrackings.Add(tracking);
            }
        }

        await destination.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    private static async Task<bool> TabelaExisteAsync(
        GameLifeDbContext context,
        string tableName,
        CancellationToken cancellationToken)
    {
        var connection = context.Database.GetDbConnection();
        var shouldClose = connection.State != ConnectionState.Open;
        if (shouldClose) await connection.OpenAsync(cancellationToken);
        try
        {
            await using var command = connection.CreateCommand();
            command.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' AND name = $name";
            var parameter = command.CreateParameter();
            parameter.ParameterName = "$name";
            parameter.Value = tableName;
            command.Parameters.Add(parameter);
            return Convert.ToInt64(await command.ExecuteScalarAsync(cancellationToken)) > 0;
        }
        finally
        {
            if (shouldClose) await connection.CloseAsync();
        }
    }

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
