using Microsoft.Extensions.Configuration;

public static class GameLifeStartup
{
    public static string ObterConnectionString(IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("GameLife");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "A configuração obrigatória 'ConnectionStrings:GameLife' não foi encontrada.");
        }

        return connectionString;
    }
}
