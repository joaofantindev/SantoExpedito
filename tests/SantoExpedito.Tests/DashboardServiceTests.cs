using SantoExpedito.Models;
using SantoExpedito.Services;

namespace SantoExpedito.Tests;

public class DashboardServiceTests
{
    private static string D(DateTime d) => $"{d.Day:D2}/{d.Month:D2}/{d.Year}";

    [Fact]
    public async Task GetDashboardDataAsync_ContabilizaDados()
    {
        using var ctx = TestHelpers.CreateContext();
        var hoje = DateTime.Now.Date;

        ctx.Clientes.AddRange(
            new Cliente { Nome = "Ana", Plano = "Mensal", Valor = 100 },
            new Cliente { Nome = "Maria", Plano = "Quinzenal", Valor = 60 });
        ctx.Pets.AddRange(
            new Pet { Nome = "Rex", Tutor = "Ana" },
            new Pet { Nome = "Luna", Tutor = "Ana" },
            new Pet { Nome = "Bob", Tutor = "Maria" });
        ctx.Vencimentos.AddRange(
            new Vencimento { Cliente = "Ana", Plano = "Mensal", VencimentoData = D(hoje), Valor = 100, Status = "Pendente" },
            new Vencimento { Cliente = "Maria", Plano = "Quinzenal", VencimentoData = D(hoje.AddDays(-2)), Valor = 60, Status = "Pendente" },
            new Vencimento { Cliente = "Ana", Plano = "Mensal", VencimentoData = D(hoje.AddDays(-30)), Valor = 100, Status = "Pago" });
        ctx.Agendamentos.AddRange(
            new Agendamento { Data = D(hoje), Pet = "Rex", Tutor = "Ana", Status = "Em espera" },
            new Agendamento { Data = D(hoje), Pet = "Luna", Tutor = "Ana", Status = "Concluído" });
        await ctx.SaveChangesAsync();

        var data = await new DashboardService(ctx).GetDashboardDataAsync();

        Assert.Equal(2, data.TotalClientes);
        Assert.Equal(3, data.TotalPets);
        Assert.Equal(1, data.PlanosMensais);
        Assert.Equal(1, data.PlanosQuinzenais);
        Assert.Equal(1, data.PagamentosPagos);
        Assert.Equal(2, data.PagamentosPendentes);
        Assert.Equal(1, data.PagamentosAtrasados);
        Assert.Equal(1, data.PagamentosVencemHoje);
        Assert.Single(data.AgendamentosHoje);
    }
}
