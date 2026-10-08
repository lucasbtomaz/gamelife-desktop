using Microsoft.EntityFrameworkCore;
using gamelife_api.Context;

var builder = WebApplication.CreateBuilder(args);

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

var connectionString = builder.Configuration.GetConnectionString("GameLife");

builder.Services.AddDbContext<GameLifeDbContext>(options =>
{
    if (!string.IsNullOrWhiteSpace(connectionString))
    {
        options.UseMySQL(connectionString);
    }
    else
    {
        // Se a connection string não estiver configurada no appsettings.json, utiliza banco em memória para testes
        options.UseInMemoryDatabase("GameLifeDb");
    }
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
}

app.UseCors("PermitirTudo");
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

await app.RunAsync();
