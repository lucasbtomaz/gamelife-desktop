using gamelife_api.Context;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace gamelife_api.Tests.Api;

public sealed class GameLifeApiFactory : WebApplicationFactory<Program>
{
    private readonly string _databaseName = $"GameLifeTests-{Guid.NewGuid()}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((_, configuration) =>
        {
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:GameLife"] = "Server=teste;Database=teste;User=teste;Password=teste"
            });
        });
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<GameLifeDbContext>>();
            services.RemoveAll<IDbContextOptionsConfiguration<GameLifeDbContext>>();
            services.RemoveAll<GameLifeDbContext>();
            services.AddDbContext<GameLifeDbContext>(options => options.UseInMemoryDatabase(_databaseName));
        });
    }
}
