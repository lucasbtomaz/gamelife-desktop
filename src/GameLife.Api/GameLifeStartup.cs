using Microsoft.Extensions.Configuration;

public static class GameLifeStartup
{
    public const string ProvedorSqlite = "Sqlite";
    public const string ProvedorMySql = "MySql";

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

    public static string ObterProvedor(IConfiguration configuration)
    {
        var provider = configuration["DatabaseProvider"];
        return string.Equals(provider, ProvedorSqlite, StringComparison.OrdinalIgnoreCase)
            ? ProvedorSqlite
            : ProvedorMySql;
    }
}
