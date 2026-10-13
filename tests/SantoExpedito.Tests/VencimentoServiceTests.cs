using SantoExpedito.Data;
using SantoExpedito.Models;
using SantoExpedito.Services;

namespace SantoExpedito.Tests;

public class VencimentoServiceTests
{
    private static string D(DateTime d) => $"{d.Day:D2}/{d.Month:D2}/{d.Year}";

    [Fact]
    public void CalcularVencimento_Mensal_Adiciona30Dias()
    {
        var inicio = new DateTime(2026, 1, 1);
        Assert.Equal("31/01/2026", VencimentoService.CalcularVencimento(inicio, "Mensal"));
    }

    [Fact]
    public void CalcularVencimento_Quinzenal_Adiciona15Dias()
    {
        var inicio = new DateTime(2026, 1, 1);
        Assert.Equal("16/01/2026", VencimentoService.CalcularVencimento(inicio, "Quinzenal"));
    }

    [Fact]
    public void CalcularVencimento_PlanoNulo_AssumeMensal()
    {
        var inicio = new DateTime(2026, 1, 1);
        Assert.Equal("31/01/2026", VencimentoService.CalcularVencimento(inicio, null));
    }

    [Fact]
    public async Task GetVencimentosAsync_ClassificaTodosOsStatus()
    {
        using var ctx = TestHelpers.CreateContext();
        var hoje = DateTime.Now.Date;

        ctx.Vencimentos.AddRange(
            new Vencimento { Cliente = "EmDia", Plano = "Mensal", VencimentoData = D(hoje.AddDays(10)), Valor = 10, Status = "Pendente" },
            new Vencimento { Cliente = "Atrasado", Plano = "Mensal", VencimentoData = D(hoje.AddDays(-1)), Valor = 10, Status = "Pendente" },
            new Vencimento { Cliente = "Hoje", Plano = "Mensal", VencimentoData = D(hoje), Valor = 10, Status = "Pendente" },
            new Vencimento { Cliente = "Prox7", Plano = "Mensal", VencimentoData = D(hoje.AddDays(3)), Valor = 10, Status = "Pendente" },
            new Vencimento { Cliente = "Pago", Plano = "Mensal", VencimentoData = D(hoje), Valor = 10, Status = "Pago" });
        await ctx.SaveChangesAsync();

        var service = new VencimentoService(ctx);

        var result = await service.GetVencimentosAsync();

        Assert.Equal(5, result.Todos.Count);
        Assert.Single(result.Pagos);
        Assert.Single(result.Atrasados);
        Assert.Equal(3, result.Pendentes.Count);
        Assert.Equal(2, result.VencemHoje.Count);
        Assert.Equal(2, result.VencemProximos7Dias.Count);
        Assert.Equal("Atrasado", result.Todos.First(v => v.Cliente == "Atrasado").Status);
    }

    [Fact]
    public async Task AlterarStatusAsync_AtualizaStatus()
    {
        using var ctx = TestHelpers.CreateContext();
        var v = new Vencimento { Cliente = "X", Plano = "Mensal", Valor = 1, Status = "Pendente" };
        ctx.Vencimentos.Add(v);
        await ctx.SaveChangesAsync();

        await new VencimentoService(ctx).AlterarStatusAsync(v.Id, "Pago");

        Assert.Equal("Pago", ctx.Vencimentos.Single().Status);
    }

    [Fact]
    public async Task RenovarPlanoAsync_ResetaVencimentoBanhosEStatus()
    {
        using var ctx = TestHelpers.CreateContext();
        var v = new Vencimento
        {
            Cliente = "Tutor",
            Plano = "Quinzenal",
            DataInicio = "01/01/2020",
            VencimentoData = "16/01/2020",
            Valor = 100,
            Status = "Atrasado"
        };
        ctx.Clientes.Add(new Cliente { Nome = "Tutor", Plano = "Quinzenal", BanhosRestantes = 0 });
        ctx.Vencimentos.Add(v);
        await ctx.SaveChangesAsync();

        await new VencimentoService(ctx).RenovarPlanoAsync(v.Id);

        var atualizado = ctx.Vencimentos.Single();
        var hoje = DateTime.Now.Date;
        Assert.Equal("Pago", atualizado.Status);
        Assert.Equal(D(hoje), atualizado.DataInicio);
        Assert.Equal(D(hoje.AddDays(15)), atualizado.VencimentoData);
        Assert.Equal(2, ctx.Clientes.Single().BanhosRestantes);
    }

    [Fact]
    public async Task GetVencimentoClienteAsync_RetornaMaisRecente()
    {
        using var ctx = TestHelpers.CreateContext();
        ctx.Vencimentos.AddRange(
            new Vencimento { Cliente = "T", Plano = "Mensal", Valor = 1, Status = "Pago" },
            new Vencimento { Cliente = "T", Plano = "Mensal", Valor = 2, Status = "Pendente" });
        await ctx.SaveChangesAsync();

        var result = await new VencimentoService(ctx).GetVencimentoClienteAsync("T");

        Assert.NotNull(result);
        Assert.Equal(2, result!.Valor);
    }

    [Fact]
    public async Task RemoverOrfaosAsync_RemoveSomenteSemCliente()
    {
        using var ctx = TestHelpers.CreateContext();
        ctx.Clientes.Add(new Cliente { Nome = "Existe" });
        ctx.Vencimentos.AddRange(
            new Vencimento { Cliente = "Existe", Valor = 1 },
            new Vencimento { Cliente = "Fantasma", Valor = 1 });
        await ctx.SaveChangesAsync();

        var removidos = await new VencimentoService(ctx).RemoverOrfaosAsync();

        Assert.Equal(1, removidos);
        Assert.Equal("Existe", ctx.Vencimentos.Single().Cliente);
    }
}
