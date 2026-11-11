using System.Text;
using gamelife_api.Context;
using gamelife_api.Contracts;
using gamelife_api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace gamelife_api.Services;

public sealed class BibliotecaService
{
    private static readonly SemaphoreSlim EscritaGate = new(1, 1);
    private readonly GameLifeDbContext _contexto;

    public BibliotecaService(GameLifeDbContext contexto) => _contexto = contexto;

    public static IQueryable<ItemBibliotecaPlataformaProjection> CriarConsultaPlana(
        GameLifeDbContext contexto,
        int? gameId = null)
    {
        var ownerships = contexto.PossesJogos.AsNoTracking();
        if (gameId.HasValue)
            ownerships = ownerships.Where(item => item.JogoId == gameId.Value);

        return from posse in ownerships
            join acompanhamento in contexto.AcompanhamentosJogos.AsNoTracking()
                on new { posse.JogoId, posse.CanalId }
                equals new { acompanhamento.JogoId, acompanhamento.CanalId }
                into acompanhamentos
            from acompanhamento in acompanhamentos.DefaultIfEmpty()
            orderby posse.Jogo.Titulo, posse.Canal.Nome
            select new ItemBibliotecaPlataformaProjection(
                posse.Id,
                posse.JogoId,
                posse.Jogo.Titulo,
                posse.Jogo.AnoLancamento,
                posse.CanalId,
                posse.Canal.Nome,
                posse.MidiaFisica,
                acompanhamento == null ? null : acompanhamento.Id,
                acompanhamento == null
                    ? null
                    : acompanhamento.Status == StatusAcompanhamentoJogo.Zerado
                        ? "Zerado"
                        : acompanhamento.Status == StatusAcompanhamentoJogo.Dropado
                            ? "Dropado"
                            : "NaoTerminado",
                acompanhamento == null ? null : acompanhamento.DataStatus,
                acompanhamento == null ? null : acompanhamento.TempoEmHoras,
                acompanhamento == null ? null : acompanhamento.ConquistasPossiveis,
                acompanhamento == null ? null : acompanhamento.ConquistasObtidas);
    }

    public async Task<IReadOnlyList<JogoBibliotecaResponse>> ObterAsync(
        CancellationToken cancellationToken)
    {
        var rows = await CriarConsultaPlana(_contexto).ToListAsync(cancellationToken);
        return Agrupar(rows);
    }

    public async Task<SalvarJogoBibliotecaResult> SalvarAsync(
        SalvarJogoBibliotecaRequest request,
        CancellationToken cancellationToken)
    {
        Validar(request);
        await EscritaGate.WaitAsync(cancellationToken);
        IDbContextTransaction? transaction = null;
        var transactionCommitted = false;
        try
        {
            if (_contexto.Database.IsRelational())
                transaction = await _contexto.Database.BeginTransactionAsync(cancellationToken);

            var title = request.Titulo.Trim();
            var titleKey = NormalizarTitulo(title);
            var games = await _contexto.Jogos.ToListAsync(cancellationToken);
            var game = games.FirstOrDefault(item => NormalizarTitulo(item.Titulo) == titleKey);
            var created = game is null;
            if (game is null)
            {
                game = new Jogo { Titulo = title, AnoLancamento = request.AnoLancamento };
                _contexto.Jogos.Add(game);
            }
            else if (request.AnoLancamento.HasValue)
            {
                game.AnoLancamento = request.AnoLancamento;
            }

            var channels = await _contexto.Canais.ToListAsync(cancellationToken);
            foreach (var platformRequest in request.Plataformas)
            {
                var platformName = platformRequest.Plataforma.Trim();
                var platformKey = NormalizarChave(platformName);
                var channel = channels.FirstOrDefault(item => NormalizarChave(item.Nome) == platformKey);
                if (channel is null)
                {
                    channel = new Canal
                    {
                        Nome = platformName,
                        PermitePosse = true,
                        PermitePreco = false
                    };
                    channels.Add(channel);
                    _contexto.Canais.Add(channel);
                }
                else
                {
                    channel.PermitePosse = true;
                }

                var ownership = game.Id == 0 || channel.Id == 0
                    ? null
                    : await _contexto.PossesJogos.FirstOrDefaultAsync(
                        item => item.JogoId == game.Id && item.CanalId == channel.Id,
                        cancellationToken);
                if (ownership is null)
                {
                    ownership = new PosseJogo
                    {
                        Jogo = game,
                        Canal = channel,
                        MidiaFisica = platformRequest.MidiaFisica,
                        AdquiridoEm = DateTime.UtcNow
                    };
                    _contexto.PossesJogos.Add(ownership);
                }
                else
                {
                    ownership.MidiaFisica = platformRequest.MidiaFisica;
                }

                if (platformRequest.Acompanhamento is not null)
                {
                    var tracking = game.Id == 0 || channel.Id == 0
                        ? null
                        : await _contexto.AcompanhamentosJogos.FirstOrDefaultAsync(
                            item => item.JogoId == game.Id && item.CanalId == channel.Id,
                            cancellationToken);
                    if (tracking is null)
                    {
                        tracking = new AcompanhamentoJogo { Jogo = game, Canal = channel };
                        _contexto.AcompanhamentosJogos.Add(tracking);
                    }

                    AplicarAcompanhamento(tracking, ownership, platformRequest.Acompanhamento);
                }
            }

            await _contexto.SaveChangesAsync(cancellationToken);
            var response = await ObterJogoAsync(game.Id, cancellationToken);
            if (transaction is not null)
            {
                await transaction.CommitAsync(cancellationToken);
                transactionCommitted = true;
            }

            return new SalvarJogoBibliotecaResult(response, created);
        }
        catch
        {
            if (transaction is not null && !transactionCommitted)
            {
                try
                {
                    await transaction.RollbackAsync(CancellationToken.None);
                }
                catch
                {
                    // A falha original é mais importante que uma falha ao desfazer a transação.
                }
            }
            throw;
        }
        finally
        {
            if (transaction is not null)
                await transaction.DisposeAsync();
            EscritaGate.Release();
        }
    }

    private async Task<JogoBibliotecaResponse> ObterJogoAsync(
        int gameId,
        CancellationToken cancellationToken)
    {
        var rows = await CriarConsultaPlana(_contexto, gameId).ToListAsync(cancellationToken);
        return Agrupar(rows).Single();
    }

    private static IReadOnlyList<JogoBibliotecaResponse> Agrupar(
        IReadOnlyList<ItemBibliotecaPlataformaProjection> rows) =>
        rows.GroupBy(item => new { item.JogoId, item.Titulo, item.AnoLancamento })
            .Select(group => new JogoBibliotecaResponse(
                group.Key.JogoId,
                group.Key.Titulo,
                group.Key.AnoLancamento,
                group.Select(item => new PlataformaBibliotecaResponse(
                    item.PosseId,
                    item.CanalId,
                    item.Plataforma,
                    item.MidiaFisica,
                    item.AcompanhamentoId.HasValue
                        ? new AcompanhamentoBibliotecaResponse(
                            item.AcompanhamentoId.Value,
                            item.Status!,
                            item.DataStatus,
                            item.TempoEmHoras,
                            item.ConquistasPossiveis,
                            item.ConquistasObtidas)
                        : null)).ToArray()))
            .ToArray();

    private static void Validar(SalvarJogoBibliotecaRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Titulo))
            throw new BibliotecaValidationException("Informe o nome do jogo.");
        if (request.Plataformas is null || request.Plataformas.Count == 0)
            throw new BibliotecaValidationException("Selecione pelo menos uma plataforma.");

        var platformKeys = new HashSet<string>(StringComparer.Ordinal);
        foreach (var platform in request.Plataformas)
        {
            if (string.IsNullOrWhiteSpace(platform.Plataforma))
                throw new BibliotecaValidationException("Informe o nome de todas as plataformas.");
            var key = NormalizarChave(platform.Plataforma);
            if (!platformKeys.Add(key))
            {
                throw new BibliotecaValidationException(
                    $"A plataforma {platform.Plataforma.Trim()} foi informada mais de uma vez.");
            }

            if (platform.Acompanhamento is not null)
                ValidarAcompanhamento(platform.Acompanhamento);
        }
    }

    private static void ValidarAcompanhamento(SalvarAcompanhamentoBibliotecaRequest request)
    {
        if (!string.IsNullOrWhiteSpace(request.Status) &&
            (int.TryParse(request.Status, out _) ||
             !Enum.TryParse<StatusAcompanhamentoJogo>(request.Status, true, out var status) ||
             !Enum.IsDefined(status)))
        {
            throw new BibliotecaValidationException("Informe um status válido.");
        }
        if (request.TempoEmHoras < 0 || request.ConquistasPossiveis < 0 || request.ConquistasObtidas < 0)
            throw new BibliotecaValidationException("Horas e conquistas não podem ser negativas.");
        if (request.ConquistasObtidas.HasValue && !request.ConquistasPossiveis.HasValue)
            throw new BibliotecaValidationException("Informe as conquistas possíveis.");
        if (request.ConquistasObtidas > request.ConquistasPossiveis)
            throw new BibliotecaValidationException("Conquistas obtidas não podem superar as possíveis.");
    }

    private static void AplicarAcompanhamento(
        AcompanhamentoJogo tracking,
        PosseJogo ownership,
        SalvarAcompanhamentoBibliotecaRequest request)
    {
        tracking.PosseJogo = ownership;
        tracking.Status = string.IsNullOrWhiteSpace(request.Status)
            ? StatusAcompanhamentoJogo.NaoTerminado
            : Enum.Parse<StatusAcompanhamentoJogo>(request.Status, true);
        tracking.DataStatus = request.DataStatus;
        tracking.TempoEmHoras = request.TempoEmHoras;
        tracking.ConquistasPossiveis = request.ConquistasPossiveis;
        tracking.ConquistasObtidas = request.ConquistasObtidas;
    }

    public static string NormalizarChave(string value) =>
        value.Trim().Normalize(NormalizationForm.FormKC).ToUpperInvariant();

    public static string NormalizarTitulo(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return string.Empty;
        var normalized = value.Trim().Normalize(NormalizationForm.FormKC).ToUpperInvariant();
        var characters = normalized.Select(character => char.IsLetterOrDigit(character) ? character : ' ').ToArray();
        return string.Join(" ", new string(characters).Split([' '], StringSplitOptions.RemoveEmptyEntries));
    }
}

public sealed record SalvarJogoBibliotecaResult(JogoBibliotecaResponse Jogo, bool Criado);

public sealed class BibliotecaValidationException(string message) : Exception(message);
