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

    public MainWindow()
    {
        InitializeComponent();
        Loaded += Inicializar;
        Closing += Encerrar;
    }

    private async void Inicializar(object sender, RoutedEventArgs e)
    {
        try
        {
            var connectionString = LocalConfiguration.ObterConnectionString();
            _aplicacao = GameLifeApplication.Criar(
                [$"--ConnectionStrings:GameLife={connectionString}"],
                builder => builder.WebHost.UseUrls("http://127.0.0.1:0"));

            await _aplicacao.StartAsync();
            var endereco = _aplicacao.Services.GetRequiredService<IServer>()
                .Features.Get<IServerAddressesFeature>()?.Addresses.SingleOrDefault()
                ?? throw new InvalidOperationException("A API local não informou seu endereço.");
            _origemLocal = new Uri(endereco);

            using var client = new HttpClient { BaseAddress = _origemLocal };
            var health = await client.GetAsync("/health");
            if (!health.IsSuccessStatusCode)
                throw new InvalidOperationException("O MySQL não respondeu corretamente.");

            var cache = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "GameLife",
                "webview");
            var ambiente = await CoreWebView2Environment.CreateAsync(userDataFolder: cache);
            await Browser.EnsureCoreWebView2Async(ambiente);
            ConfigurarWebView(Browser.CoreWebView2);
            Browser.CoreWebView2.Navigate(_origemLocal.ToString());
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                "Não foi possível abrir o GameLife.\n\n" + ex.Message +
                "\n\nVerifique se o MySQL e o Microsoft Edge WebView2 Runtime estão instalados.",
                "GameLife",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            await PararAplicacaoAsync();
            Application.Current.Shutdown(1);
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
        web.NewWindowRequested += (_, args) => args.Handled = true;
        web.PermissionRequested += (_, args) => args.State = CoreWebView2PermissionState.Deny;
    }

    private async void Encerrar(object? sender, CancelEventArgs e)
    {
        if (_encerrando) return;
        e.Cancel = true;
        _encerrando = true;
        IsEnabled = false;
        Browser.Dispose();
        await PararAplicacaoAsync();
        Application.Current.Shutdown();
    }

    private async Task PararAplicacaoAsync()
    {
        if (_aplicacao is null) return;
        using var limite = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        await _aplicacao.StopAsync(limite.Token);
        await _aplicacao.DisposeAsync();
        _aplicacao = null;
    }
}
