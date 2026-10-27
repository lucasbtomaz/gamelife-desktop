using Microsoft.Extensions.Configuration;

public static class GameLifeStartup
{
    public static string ObterConnectionString(IConfiguration configuration)
    {
        return configuration.GetConnectionString("GameLife")
            ?? throw new InvalidOperationException(
                "A configuração obrigatória 'ConnectionStrings:GameLife' não foi encontrada.");
    }
}
