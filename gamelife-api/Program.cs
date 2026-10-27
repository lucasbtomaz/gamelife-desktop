using Microsoft.EntityFrameworkCore;
using gamelife_api.Context;

var builder = WebApplication.CreateBuilder(new WebApplicationOptions
{
    Args = args,
    WebRootPath = Path.Combine(AppContext.BaseDirectory, "wwwroot")
});

// Adicionar controllers da API
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// Configuração de CORS para permitir requisições de frontends locais ou hospedados
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

builder.Services.AddDbContext<GameLifeDbContext>(options =>
{
    options.UseMySQL(connectionString);
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
}

app.UseCors("PermitirTudo");
app.UseHttpsRedirection();
app.UseAuthorization();
app.UseDefaultFiles();
app.UseStaticFiles();
app.MapControllers();
app.MapFallbackToFile("index.html");

await app.RunAsync();

public partial class Program;
