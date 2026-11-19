using System.Globalization;
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
                posse.CompraDigital,
                posse.MidiaFisica,
                posse.PorAssinatura,
                posse.ResgatadoDeAssinatura,
                posse.ServicoExigido,
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
        var gameIds = rows.Select(item => item.JogoId).Distinct().ToArray();
        var subscriptions = gameIds.Length == 0
            ? []
            : await _contexto.AssinaturasJogos.AsNoTracking()
                .Where(item => gameIds.Contains(item.JogoId))
                .OrderBy(item => item.Servico)
                .ToListAsync(cancellationToken);
        var subscriptionsByGame = subscriptions
            .GroupBy(item => item.JogoId)
            .ToDictionary(group => group.Key, group => (IReadOnlyList<AssinaturaJogoResponse>)group.Select(ProjetarAssinatura).ToArray());

        return Agrupar(rows, subscriptionsByGame, await ObterServicosAtivosAsync(cancellationToken));
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
            var anyNewOwnership = false;

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

                var (compraDigital, midiaFisica, porAssinatura) = ResolverFormasDeAcesso(platformRequest);

                var ownership = game.Id == 0 || channel.Id == 0
                    ? null
                    : await _contexto.PossesJogos.FirstOrDefaultAsync(
                        item => item.JogoId == game.Id && item.CanalId == channel.Id,
                        cancellationToken);

                var eraDaBiblioteca = ownership is not null && !AcessoPosse.EhCatalogo(
                    ownership.CompraDigital, ownership.MidiaFisica, ownership.PorAssinatura, ownership.ResgatadoDeAssinatura);
                if (ownership is null)
                {
                    ownership = new PosseJogo
                    {
                        Jogo = game,
                        Canal = channel,
                        AdquiridoEm = DateTime.UtcNow
                    };
                    _contexto.PossesJogos.Add(ownership);
                }

                ownership.CompraDigital = compraDigital;
                ownership.MidiaFisica = midiaFisica;
                ownership.ResgatadoDeAssinatura = platformRequest.ResgatadoDeAssinatura ?? ownership.ResgatadoDeAssinatura;
                ownership.ServicoExigido = ownership.ResgatadoDeAssinatura
                    ? (platformRequest.ResgatadoDeAssinatura == true
                        ? NormalizarServicoExigido(platformRequest.ServicoExigido)
                        : ownership.ServicoExigido)
                    : null;
                ownership.PorAssinatura = porAssinatura || ownership.ResgatadoDeAssinatura;
                anyNewOwnership |= !eraDaBiblioteca && !AcessoPosse.EhCatalogo(
                    ownership.CompraDigital, ownership.MidiaFisica, ownership.PorAssinatura, ownership.ResgatadoDeAssinatura);

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

            if (request.Assinaturas is not null)
            {
                var existingSubscriptions = game.Id == 0
                    ? []
                    : await _contexto.AssinaturasJogos
                        .Where(item => item.JogoId == game.Id)
                        .ToListAsync(cancellationToken);

                foreach (var subscriptionRequest in request.Assinaturas)
                {
                    var servico = AssinaturaCatalogo.NormalizarServico(subscriptionRequest.Servico);
                    var situacao = AssinaturaCatalogo.NormalizarSituacao(subscriptionRequest.Situacao);
                    var existing = existingSubscriptions.FirstOrDefault(
                        item => string.Equals(item.Servico, servico, StringComparison.OrdinalIgnoreCase));

                    if (existing is null)
                    {
                        existing = new AssinaturaJogo
                        {
                            Jogo = game,
                            Servico = servico,
                            Situacao = situacao,
                            DataDisponibilidade = subscriptionRequest.DataDisponibilidade,
                            DataEncerramento = subscriptionRequest.DataEncerramento
                        };
                        _contexto.AssinaturasJogos.Add(existing);
                        existingSubscriptions.Add(existing);
                    }
                    else
                    {
                        existing.Situacao = situacao;
                        existing.DataDisponibilidade = subscriptionRequest.DataDisponibilidade;
                        existing.DataEncerramento = subscriptionRequest.DataEncerramento;
                    }
                }
            }

            if (anyNewOwnership && game.Id > 0)
            {
                var wishlistItem = await _contexto.ItensListaDesejos
                    .Include(item => item.RegistrosPrecos)
                    .FirstOrDefaultAsync(item => item.JogoId == game.Id, cancellationToken);

                if (wishlistItem is not null && wishlistItem.RegistrosPrecos.Count > 0)
                    wishlistItem.AlertaBibliotecaSilenciado = false;
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
        var subscriptions = await _contexto.AssinaturasJogos.AsNoTracking()
            .Where(item => item.JogoId == gameId)
            .OrderBy(item => item.Servico)
            .ToListAsync(cancellationToken);
        var map = new Dictionary<int, IReadOnlyList<AssinaturaJogoResponse>>
        {
            [gameId] = subscriptions.Select(ProjetarAssinatura).ToArray()
        };
        return Agrupar(rows, map, await ObterServicosAtivosAsync(cancellationToken)).Single();
    }

    private async Task<HashSet<string>> ObterServicosAtivosAsync(CancellationToken cancellationToken)
    {
        var servicos = await _contexto.MinhasAssinaturas.AsNoTracking()
            .Where(item => item.Ativa)
            .Select(item => item.Servico)
            .ToListAsync(cancellationToken);

        return servicos.ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    public static AssinaturaJogoResponse ProjetarAssinatura(AssinaturaJogo item) =>
        new(item.Id, item.JogoId, item.Servico, item.Situacao, item.DataDisponibilidade, item.DataEncerramento);

    private static IReadOnlyList<JogoBibliotecaResponse> Agrupar(
        IReadOnlyList<ItemBibliotecaPlataformaProjection> rows,
        IReadOnlyDictionary<int, IReadOnlyList<AssinaturaJogoResponse>> subscriptionsByGame,
        IReadOnlySet<string> servicosAtivos) =>
        rows.GroupBy(item => new { item.JogoId, item.Titulo, item.AnoLancamento })
            .Select(group =>
            {
                var subscriptions = subscriptionsByGame.GetValueOrDefault(group.Key.JogoId) ?? [];
                var situacoes = subscriptions.Select(item => item.Situacao).ToArray();
                return new JogoBibliotecaResponse(
                    group.Key.JogoId,
                    group.Key.Titulo,
                    group.Key.AnoLancamento,
                    group.Select(item => new PlataformaBibliotecaResponse(
                        item.PosseId,
                        item.CanalId,
                        item.Plataforma,
                        item.CompraDigital,
                        item.MidiaFisica,
                        item.PorAssinatura,
                        item.AcompanhamentoId.HasValue
                            ? new AcompanhamentoBibliotecaResponse(
                                item.AcompanhamentoId.Value,
                                item.Status!,
                                item.DataStatus,
                                item.TempoEmHoras,
                                item.ConquistasPossiveis,
                                item.ConquistasObtidas)
                            : null,
                        item.ResgatadoDeAssinatura,
                        item.ServicoExigido,
                        AcessoPosse.Classificar(item.CompraDigital, item.MidiaFisica, item.PorAssinatura,
                            item.ResgatadoDeAssinatura, item.ServicoExigido, servicosAtivos, situacoes).ToString())).ToArray(),
                    subscriptions);
            })
            .ToArray();

    private static (bool CompraDigital, bool MidiaFisica, bool PorAssinatura) ResolverFormasDeAcesso(
        SalvarPlataformaBibliotecaRequest request)
    {
        if (request.CompraDigital.HasValue || request.PorAssinatura.HasValue || request.ResgatadoDeAssinatura == true)
        {
            return (
                request.CompraDigital ?? false,
                request.MidiaFisica,
                request.PorAssinatura == true || request.ResgatadoDeAssinatura == true);
        }

        // Compatibilidade com chamadas antigas que enviavam apenas MidiaFisica
        return (!request.MidiaFisica, request.MidiaFisica, false);
    }

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

            if (platform.ResgatadoDeAssinatura == true)
                NormalizarServicoExigido(platform.ServicoExigido);

            if (platform.CompraDigital.HasValue || platform.PorAssinatura.HasValue)
            {
                var (compraDigital, midiaFisica, porAssinatura) = ResolverFormasDeAcesso(platform);
                if (!compraDigital && !midiaFisica && !porAssinatura)
                {
                    throw new BibliotecaValidationException(
                        $"Selecione pelo menos uma forma de acesso para {platform.Plataforma.Trim()}.");
                }
            }

            if (platform.Acompanhamento is not null)
                ValidarAcompanhamento(platform.Acompanhamento);
        }

        if (request.Assinaturas is not null)
        {
            var serviceKeys = new HashSet<string>(StringComparer.Ordinal);
            foreach (var subscription in request.Assinaturas)
            {
                try
                {
                    var servico = AssinaturaCatalogo.NormalizarServico(subscription.Servico);
                    AssinaturaCatalogo.NormalizarSituacao(subscription.Situacao);
                    if (!serviceKeys.Add(servico))
                    {
                        throw new BibliotecaValidationException(
                            $"A assinatura {servico} foi informada mais de uma vez para este jogo.");
                    }

                    if (subscription.DataDisponibilidade.HasValue &&
                        subscription.DataEncerramento.HasValue &&
                        subscription.DataEncerramento.Value.Date < subscription.DataDisponibilidade.Value.Date)
                    {
                        throw new BibliotecaValidationException(
                            "A data de encerramento da assinatura não pode ser anterior à data de entrada.");
                    }
                }
                catch (ArgumentException ex)
                {
                    throw new BibliotecaValidationException(ex.Message);
                }
            }
        }
    }

    private static string NormalizarServicoExigido(string? servico)
    {
        try
        {
            return AssinaturaCatalogo.NormalizarServico(servico);
        }
        catch (ArgumentException)
        {
            throw new BibliotecaValidationException("Informe o serviço de assinatura exigido para o jogo resgatado.");
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
        var decomposed = value.Trim()
            .Normalize(NormalizationForm.FormKC)
            .Normalize(NormalizationForm.FormD);
        var key = new StringBuilder(decomposed.Length);

        foreach (var character in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) == UnicodeCategory.NonSpacingMark)
                continue;
            if (char.IsLetterOrDigit(character))
                key.Append(char.ToUpperInvariant(character));
        }

        return key.ToString();
    }
}

public sealed record SalvarJogoBibliotecaResult(JogoBibliotecaResponse Jogo, bool Criado);

public sealed class BibliotecaValidationException(string message) : Exception(message);
