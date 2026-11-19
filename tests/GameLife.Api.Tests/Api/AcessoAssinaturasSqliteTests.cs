using gamelife_api.Context;
using gamelife_api.Contracts;
using gamelife_api.Controllers;
using gamelife_api.Models;
using gamelife_api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace gamelife_api.Tests.Api;

public sealed class AcessoAssinaturasSqliteTests : IAsyncLifetime
{
    private readonly SqliteConnection _conexao = new("Data Source=:memory:");
    private GameLifeDbContext _contexto = null!;
    private static readonly CancellationToken Token = CancellationToken.None;

    public async Task InitializeAsync()
    {
        await _conexao.OpenAsync();
        _contexto = new GameLifeDbContext(new DbContextOptionsBuilder<GameLifeDbContext>()
            .UseSqlite(_conexao).Options);
        await _contexto.Database.EnsureCreatedAsync();
    }

    public async Task DisposeAsync()
    {
        await _contexto.DisposeAsync();
        await _conexao.DisposeAsync();
    }

    [Fact]
    public async Task PersisteResgateEAtualizaAcessoQuandoServicoMuda()
    {
        var biblioteca = new BibliotecaService(_contexto);
        var assinaturas = new MinhasAssinaturasController(_contexto);
        var iniciais = await assinaturas.Obter(Token);
        Assert.Equal(4, iniciais.Count);
        Assert.Equal(iniciais.Select(item => item.Servico).Order(StringComparer.OrdinalIgnoreCase),
            iniciais.Select(item => item.Servico));
        Assert.All(iniciais, item => { Assert.False(item.Ativa); Assert.Null(item.ProximaRenovacao); });
        Assert.IsType<BadRequestObjectResult>((await assinaturas.Salvar(new("Netflix", true, null), Token)).Result);

        var resgate = new SalvarPlataformaBibliotecaRequest("PSN", false, ResgatadoDeAssinatura: true,
            ServicoExigido: "ps plus");
        var resultado = await biblioteca.SalvarAsync(new("Jogo mensal", null, [resgate]), Token);
        var plataforma = Assert.Single(resultado.Jogo.Plataformas);
        Assert.Equal("BloqueadoSemAssinatura", plataforma.EstadoAcesso);
        var posse = await _contexto.PossesJogos.AsNoTracking().SingleAsync();
        Assert.True(posse.PorAssinatura);
        Assert.True(posse.ResgatadoDeAssinatura);
        Assert.False(posse.CompraDigital);
        Assert.Equal("PlayStation Plus", posse.ServicoExigido);

        var renovacao = DateTime.Today.AddMonths(1);
        Assert.IsType<OkObjectResult>((await assinaturas.Salvar(new("ps plus", true, renovacao), Token)).Result);
        Assert.Equal(renovacao, (await assinaturas.Obter(Token)).Single(item => item.Ativa).ProximaRenovacao);
        Assert.Equal("JogavelPorResgate", Assert.Single(Assert.Single(await biblioteca.ObterAsync(Token)).Plataformas).EstadoAcesso);

        await assinaturas.Salvar(new("PlayStation Plus", false, null), Token);
        Assert.Equal(1, await _contexto.MinhasAssinaturas.CountAsync());
        Assert.Equal("BloqueadoSemAssinatura", Assert.Single(Assert.Single(await biblioteca.ObterAsync(Token)).Plataformas).EstadoAcesso);

        var compra = new SalvarPlataformaBibliotecaRequest("PSN", false, CompraDigital: true);
        var editado = await biblioteca.SalvarAsync(new("Jogo mensal", null, [compra]), Token);
        Assert.True(Assert.Single(editado.Jogo.Plataformas).ResgatadoDeAssinatura);
        editado = await biblioteca.SalvarAsync(new("Jogo mensal", null, [compra with { ResgatadoDeAssinatura = false }]), Token);
        Assert.Null(Assert.Single(editado.Jogo.Plataformas).ServicoExigido);
    }

    [Theory]
    [InlineData("")]
    [InlineData("Netflix")]
    public async Task ResgateInvalidoFalhaAntesDeGravar(string servico)
    {
        var biblioteca = new BibliotecaService(_contexto);
        var erro = await Assert.ThrowsAsync<BibliotecaValidationException>(() =>
            biblioteca.SalvarAsync(new("Jogo inválido", null, [
                new("PSN", false, ResgatadoDeAssinatura: true, ServicoExigido: servico)
            ]), Token));
        Assert.Contains("serviço", erro.Message);
        Assert.Empty(await _contexto.Jogos.ToListAsync());
    }

    [Fact]
    public async Task DashboardExcluiCatalogoDaBibliotecaEAlertaSomenteSaidasElegiveis()
    {
        var biblioteca = new BibliotecaService(_contexto);
        var catalogo = new SalvarPlataformaBibliotecaRequest("XBOX", false, PorAssinatura: true);
        var inicio = DateTime.Today;
        (string Titulo, int Dias, bool Compra, string? Status, string Situacao)[] casos =
        {
            ("Sai em dez dias", 10, false, null, "Disponível"),
            ("Sai no último dia", 30, false, null, "Disponível"),
            ("Sai depois", 45, false, null, "Disponível"),
            ("Saiu ontem", -1, false, null, "Disponível"),
            ("Já zerado", 10, false, "Zerado", "Disponível"),
            ("Já dropado", 10, false, "Dropado", "Disponível"),
            ("Também comprado", 10, true, null, "Disponível"),
            ("Encerrado", 10, false, null, "Encerrado")
        };
        foreach (var (titulo, dias, compra, status, situacao) in casos)
        {
            var salvo = await biblioteca.SalvarAsync(new(titulo, null, [
                catalogo with { CompraDigital = compra,
                    Acompanhamento = status is null ? null : new(status, null, null, null, null) }
            ], [new(null, null, "Game Pass", situacao, null, inicio.AddDays(dias).AddHours(20))]), Token);
            Assert.Equal(compra ? "Possuido" : situacao == "Encerrado" ? "SaiuDoCatalogo" : "Catalogo",
                Assert.Single(salvo.Jogo.Plataformas).EstadoAcesso);
        }
        await biblioteca.SalvarAsync(new("Jogo mensal", null, [
            new("PSN", false, ResgatadoDeAssinatura: true, ServicoExigido: "ps plus")
        ]), Token);

        Assert.Equal(9, (await biblioteca.ObterAsync(Token)).Count);
        var dashboard = new DashboardService(_contexto, new PerfilService(_contexto));
        var resposta = await dashboard.ObterInicioAsync(Token);
        Assert.Equal(2, resposta.Resumo.TotalJogos);
        Assert.Equal(2, resposta.JogosRecentes.Count);
        Assert.Equal(1, resposta.BibliotecaPorPlataforma.Single(p => p.Plataforma == "XBOX").Total);
        Assert.Equal([10, 30], resposta.AlertasSaidaCatalogo.Select(item => item.DiasRestantes).ToArray());
        Assert.Equal("Sai em dez dias", resposta.AlertasSaidaCatalogo[0].Titulo);
    }
}
