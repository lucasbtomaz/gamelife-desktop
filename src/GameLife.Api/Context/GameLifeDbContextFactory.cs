using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace gamelife_api.Context;

public sealed class GameLifeDbContextFactory : IDesignTimeDbContextFactory<GameLifeDbContext>
{
    public GameLifeDbContext CreateDbContext(string[] args)
    {
        var ambiente = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json")
            .AddJsonFile($"appsettings.{ambiente}.json", optional: true)
            .AddUserSecrets<Program>(optional: true)
            .AddEnvironmentVariables()
            .Build();

        var options = new DbContextOptionsBuilder<GameLifeDbContext>()
            .UseMySQL(GameLifeStartup.ObterConnectionString(configuration))
            .Options;

        return new GameLifeDbContext(options);
    }
}
