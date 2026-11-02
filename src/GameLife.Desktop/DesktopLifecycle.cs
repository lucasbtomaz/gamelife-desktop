namespace GameLife.Desktop;

public sealed class DesktopLifecycle : IDisposable
{
    private readonly CancellationTokenSource _cancelamento = new();
    private readonly object _sincronizacao = new();
    private Task _inicializacao = Task.CompletedTask;
    private Task? _encerramento;
    private bool _iniciou;

    public void Iniciar(Func<CancellationToken, Task> inicializar)
    {
        lock (_sincronizacao)
        {
            if (_iniciou) throw new InvalidOperationException("A inicialização já foi iniciada.");
            _iniciou = true;
            _inicializacao = inicializar(_cancelamento.Token);
        }
    }

    public Task EncerrarAsync(Func<Task> parar)
    {
        lock (_sincronizacao)
        {
            return _encerramento ??= EncerrarCoreAsync(parar);
        }
    }

    private async Task EncerrarCoreAsync(Func<Task> parar)
    {
        _cancelamento.Cancel();
        try
        {
            await _inicializacao;
        }
        catch (OperationCanceledException) when (_cancelamento.IsCancellationRequested)
        {
        }
        finally
        {
            await parar();
        }
    }

    public void Dispose() => _cancelamento.Dispose();
}
