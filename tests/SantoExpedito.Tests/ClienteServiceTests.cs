using SantoExpedito.Models;
using SantoExpedito.Services;

namespace SantoExpedito.Tests;

public class ClienteServiceTests
{
    private static ClienteService Create(SantoExpedito.Data.AppDbContext ctx) =>
        new(ctx, TestHelpers.CreateAgendamentoService(ctx), TestHelpers.CreateCpfService());

    [Fact]
    public async Task CreateAsync_ArmazenaCpfCifrado_EGetAllDecifra()
    {
        using var ctx = TestHelpers.CreateContext();
        var service = Create(ctx);

        await service.CreateAsync(new Cliente { Nome = "Ana", Cpf = "111.222.333-44" });

        var cifrado = ctx.Clientes.Single().Cpf;
        Assert.NotEqual("111.222.333-44", cifrado);

        var listados = await service.GetAllAsync();
        Assert.Equal("111.222.333-44", listados.Single().Cpf);
    }

    [Fact]
    public async Task GetAllAsync_FiltraPorTermo()
    {
        using var ctx = TestHelpers.CreateContext();
        var service = Create(ctx);
        await service.CreateAsync(new Cliente { Nome = "Carlos", PetNome = "Rex" });
        await service.CreateAsync(new Cliente { Nome = "Maria", PetNome = "Luna" });

        var resultado = await service.GetAllAsync("rex");

        Assert.Single(resultado);
        Assert.Equal("Carlos", resultado[0].Nome);
    }

    [Fact]
    public async Task DeleteAsync_RemoveDadosRelacionados()
    {
        using var ctx = TestHelpers.CreateContext();
        var service = Create(ctx);

        ctx.Clientes.Add(new Cliente { Nome = "Tutor", PetNome = "Pet" });
        ctx.Pets.Add(new Pet { Nome = "Pet", Tutor = "Tutor" });
        ctx.Agendamentos.Add(new Agendamento { Pet = "Pet", Tutor = "Tutor" });
        ctx.Vencimentos.Add(new Vencimento { Cliente = "Tutor" });
        await ctx.SaveChangesAsync();
        var id = ctx.Clientes.Single().Id;

        await service.DeleteAsync(id);

        Assert.Empty(ctx.Clientes);
        Assert.Empty(ctx.Pets);
        Assert.Empty(ctx.Agendamentos);
        Assert.Empty(ctx.Vencimentos);
    }

    [Fact]
    public async Task LimparMockDataAsync_RemoveSomenteMock()
    {
        using var ctx = TestHelpers.CreateContext();
        var service = Create(ctx);

        ctx.Clientes.AddRange(
            new Cliente { Nome = "[MOCK] Ana" },
            new Cliente { Nome = "Real" });
        ctx.Pets.AddRange(
            new Pet { Nome = "[MOCK] Rex", Tutor = "[MOCK] Ana" },
            new Pet { Nome = "Verdadeiro", Tutor = "Real" });
        ctx.Agendamentos.Add(new Agendamento { Pet = "[MOCK] Rex", Tutor = "[MOCK] Ana" });
        ctx.Vencimentos.Add(new Vencimento { Cliente = "[MOCK] Ana" });
        await ctx.SaveChangesAsync();

        var removidos = await service.LimparMockDataAsync();

        Assert.Equal(1, removidos);
        Assert.Equal("Real", ctx.Clientes.Single().Nome);
        Assert.Single(ctx.Pets);
        Assert.Empty(ctx.Agendamentos);
        Assert.Empty(ctx.Vencimentos);
    }

    [Fact]
    public async Task GerarMockDataAsync_CriaBaseEsperada()
    {
        using var ctx = TestHelpers.CreateContext();
        var service = Create(ctx);

        var mensagem = await service.GerarMockDataAsync();

        Assert.Contains("300 clientes", mensagem);
        Assert.Equal(900, ctx.Clientes.Count());
        Assert.Equal(900, ctx.Pets.Count());
    }
}
