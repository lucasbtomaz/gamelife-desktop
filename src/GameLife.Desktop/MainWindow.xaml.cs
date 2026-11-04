using System.ComponentModel;
using System.IO;
using System.Net.Http;
using System.Windows;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Web.WebView2.Core;

namespace GameLife.Desktop;

public partial class MainWindow : Window
{
    private WebApplication? _aplicacao;
    private Uri? _origemLocal;
    private bool _encerrando;
    private readonly DesktopLifecycle _lifecycle = new();
    private readonly SemaphoreSlim _pararGate = new(1, 1);

    public MainWindow()
    {
        InitializeComponent();
        Loaded += (_, _) => _lifecycle.Iniciar(InicializarAsync);
        Closing += Encerrar;
    }

    private async Task InicializarAsync(CancellationToken cancellationToken)
    {
        try
        {
            DesktopAssets.Validar(Path.Combine(AppContext.BaseDirectory, "wwwroot"));
            var databasePath = LocalDatabase.CaminhoPadrao;
            var legacyConnectionString = File.Exists(databasePath)
                ? null
                : LocalConfiguration.TentarObterConnectionString();
            await LocalDatabase.PrepararAsync(
                databasePath,
                legacyConnectionString,
                cancellationToken,
                LocalDatabase.CaminhoLegado);
            var connectionString = LocalDatabase.ObterConnectionString(databasePath);
            _aplicacao = GameLifeApplication.Criar(
                [
                    $"--ConnectionStrings:GameLife={connectionString}",
                    $"--DatabaseProvider={GameLifeStartup.ProvedorSqlite}"
                ],
                builder => builder.WebHost.UseUrls("http://127.0.0.1:0"));

            await _aplicacao.StartAsync(cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            var endereco = _aplicacao.Services.GetRequiredService<IServer>()
                .Features.Get<IServerAddressesFeature>()?.Addresses.SingleOrDefault()
                ?? throw new InvalidOperationException("A API local não informou seu endereço.");
            _origemLocal = new Uri(endereco);

            using var client = new HttpClient { BaseAddress = _origemLocal };
            var health = await client.GetAsync("/health", cancellationToken);
            if (!health.IsSuccessStatusCode)
                throw new InvalidOperationException("O banco local não respondeu corretamente.");

            var cache = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                "AppData",
                "Local",
                "GameLife",
                "webview");
            var ambiente = await CoreWebView2Environment.CreateAsync(userDataFolder: cache);
            await Browser.EnsureCoreWebView2Async(ambiente);
            cancellationToken.ThrowIfCancellationRequested();
            ConfigurarWebView(Browser.CoreWebView2);
            Browser.CoreWebView2.Navigate(_origemLocal.ToString());
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception ex)
        {
            await PararAplicacaoAsync();
            if (!_encerrando)
            {
                MessageBox.Show(
                    "Não foi possível abrir o GameLife.\n\n" + ex.Message +
                    "\n\nVerifique se o Microsoft Edge WebView2 Runtime está instalado.",
                    "GameLife",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                Application.Current.Shutdown(1);
            }
        }
    }

    private void ConfigurarWebView(CoreWebView2 web)
    {
        web.Settings.AreDevToolsEnabled = false;
        web.Settings.AreDefaultContextMenusEnabled = false;
        web.Settings.IsStatusBarEnabled = false;
        web.NavigationStarting += (_, args) =>
        {
            if (_origemLocal is null || !LocalNavigation.EhPermitida(_origemLocal, args.Uri))
                args.Cancel = true;
        };
        web.FrameNavigationStarting += (_, args) =>
        {
            if (_origemLocal is null || !LocalNavigation.EhPermitida(_origemLocal, args.Uri))
                args.Cancel = true;
        };
        web.NewWindowRequested += (_, args) => args.Handled = true;
        web.PermissionRequested += (_, args) => args.State = CoreWebView2PermissionState.Deny;
    }

    private async void Encerrar(object? sender, CancelEventArgs e)
    {
        if (_encerrando) return;
        e.Cancel = true;
        _encerrando = true;
        IsEnabled = false;
        try
        {
            await _lifecycle.EncerrarAsync(PararAplicacaoAsync);
        }
        finally
        {
            Browser.Dispose();
            _lifecycle.Dispose();
            _pararGate.Dispose();
            Application.Current.Shutdown();
        }
    }

    private async Task PararAplicacaoAsync()
    {
        await _pararGate.WaitAsync();
        try
        {
            if (_aplicacao is null) return;
            var aplicacao = _aplicacao;
            _aplicacao = null;
            using var limite = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            await aplicacao.StopAsync(limite.Token);
            await aplicacao.DisposeAsync();
        }
        finally
        {
            _pararGate.Release();
        }
    }
}
