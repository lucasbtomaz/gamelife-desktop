using gamelife_api.Context;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace gamelife_api.Tests.Hosting;

public sealed class GameLifeApplicationTests
{
    [Fact]
    public async Task CriarConfiguraControllersEHealth()
    {
        await using var app = GameLifeApplication.Criar(
            ["--ConnectionStrings:GameLife=Server=teste;Database=teste;User=teste;Password=teste"],
            builder =>
        {
            builder.WebHost.UseUrls("http://127.0.0.1:0");
            builder.Services.RemoveAll<DbContextOptions<GameLifeDbContext>>();
            builder.Services.RemoveAll<IDbContextOptionsConfiguration<GameLifeDbContext>>();
            builder.Services.RemoveAll<GameLifeDbContext>();
            builder.Services.AddDbContext<GameLifeDbContext>(options =>
                options.UseInMemoryDatabase($"GameLifeApplication-{Guid.NewGuid()}"));
        });

        await app.StartAsync();
        var endereco = app.Services.GetRequiredService<IServer>()
            .Features.Get<IServerAddressesFeature>()!.Addresses.Single();

        using var client = new HttpClient { BaseAddress = new Uri(endereco) };
        var response = await client.GetAsync("/health");

        response.EnsureSuccessStatusCode();
        Assert.Contains("\"status\":\"ready\"", await response.Content.ReadAsStringAsync());

        await app.StopAsync();
    }
}
