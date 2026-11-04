using gamelife_api.Context;
using Microsoft.EntityFrameworkCore;

public static class GameLifeApplication
{
    public static WebApplication Criar(
        string[] args,
        Action<WebApplicationBuilder>? configurarBuilder = null)
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            Args = args,
            ApplicationName = typeof(GameLifeApplication).Assembly.FullName,
            WebRootPath = Path.Combine(AppContext.BaseDirectory, "wwwroot")
        });

        builder.Services.AddControllers();
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddCors(options =>
        {
            options.AddPolicy("PermitirTudo", politica =>
            {
                politica.AllowAnyOrigin()
                    .AllowAnyMethod()
                    .AllowAnyHeader();
            });
        });

        var connectionString = GameLifeStartup.ObterConnectionString(builder.Configuration);
        var provider = GameLifeStartup.ObterProvedor(builder.Configuration);
        builder.Services.AddDbContext<GameLifeDbContext>(options =>
        {
            if (provider == GameLifeStartup.ProvedorSqlite)
                options.UseSqlite(connectionString);
            else
                options.UseMySQL(connectionString);
        });

        configurarBuilder?.Invoke(builder);

        var app = builder.Build();
        app.UseCors("PermitirTudo");
        app.UseHttpsRedirection();
        app.UseAuthorization();
        app.UseDefaultFiles();
        app.UseStaticFiles();
        app.MapControllers();
        app.MapFallbackToFile("index.html");
        return app;
    }
}
