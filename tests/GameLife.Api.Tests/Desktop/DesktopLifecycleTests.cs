using GameLife.Desktop;

namespace gamelife_api.Tests.Desktop;

public sealed class DesktopLifecycleTests
{
    [Fact]
    public async Task EncerrarCancelaInicializacaoEAguardaAntesDeParar()
    {
        var iniciou = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var cancelou = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var paradas = 0;
        var lifecycle = new DesktopLifecycle();
        lifecycle.Iniciar(async cancellationToken =>
        {
            iniciou.SetResult();
            try
            {
                await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            }
            finally
            {
                cancelou.SetResult();
            }
        });
        await iniciou.Task;

        await Task.WhenAll(
            lifecycle.EncerrarAsync(() => { Interlocked.Increment(ref paradas); return Task.CompletedTask; }),
            lifecycle.EncerrarAsync(() => { Interlocked.Increment(ref paradas); return Task.CompletedTask; }));

        Assert.True(cancelou.Task.IsCompleted);
        Assert.Equal(1, paradas);
    }
}
