namespace GameLife.Desktop;

public static class LocalNavigation
{
    public static bool EhPermitida(Uri origem, string destino)
        => Uri.TryCreate(destino, UriKind.Absolute, out var uri) &&
           uri.Scheme == origem.Scheme &&
           uri.Host == origem.Host &&
           uri.Port == origem.Port;
}
