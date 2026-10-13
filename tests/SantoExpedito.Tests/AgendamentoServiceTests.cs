using SantoExpedito.Models;

namespace SantoExpedito.Tests;

public class AgendamentoServiceTests
{
    [Fact]
    public async Task GerarAgendamentosRecorrentes_CriaAgendamentosForaDoDomingo()
    {
        using var ctx = TestHelpers.CreateContext();
        var service = TestHelpers.CreateAgendamentoService(ctx);

        await service.GerarAgendamentosRecorrentesAsync("Ana", "Rex", "Mensal", "10,20", 3);

        var agendamentos = ctx.Agendamentos.ToList();
        Assert.Equal(6, agendamentos.Count);
        Assert.All(agendamentos, a => Assert.Equal("08:00", a.Horario));
        Assert.All(agendamentos, a => Assert.Equal("Em espera", a.Status));
        Assert.All(agendamentos, a =>
        {
            var data = DateTime.ParseExact(a.Data!, "dd/MM/yyyy", null);
            Assert.NotEqual(DayOfWeek.Sunday, data.DayOfWeek);
        });
    }

    [Fact]
    public async Task GerarAgendamentosRecorrentes_NaoDuplicaAoExecutarNovamente()
    {
        using var ctx = TestHelpers.CreateContext();
        var service = TestHelpers.CreateAgendamentoService(ctx);

        await service.GerarAgendamentosRecorrentesAsync("Ana", "Rex", "Mensal", "10", 3);
        var primeira = ctx.Agendamentos.Count();

        await service.GerarAgendamentosRecorrentesAsync("Ana", "Rex", "Mensal", "10", 3);

        Assert.Equal(primeira, ctx.Agendamentos.Count());
    }

    [Fact]
    public async Task CriarBanhoUnicoAsync_PersisteComPlanoUnico()
    {
        using var ctx = TestHelpers.CreateContext();
        var service = TestHelpers.CreateAgendamentoService(ctx);

        await service.CriarBanhoUnicoAsync("Ana", "Rua 1", "9999", "Rex", "10/10/2026", "09:00");

        var a = ctx.Agendamentos.Single();
        Assert.Equal("Único", a.Plano);
        Assert.Equal("Em espera", a.Status);
        Assert.Equal("Rua 1", a.Endereco);
    }

    [Fact]
    public async Task RemoverOrfaosAsync_RemoveSemPetOuCliente()
    {
        using var ctx = TestHelpers.CreateContext();
        var service = TestHelpers.CreateAgendamentoService(ctx);

        ctx.Clientes.Add(new Cliente { Nome = "Ana", PetNome = "Rex" });
        ctx.Pets.Add(new Pet { Nome = "Rex", Tutor = "Ana" });
        ctx.Agendamentos.AddRange(
            new Agendamento { Pet = "Rex", Tutor = "Ana" },
            new Agendamento { Pet = "Fantasma", Tutor = "Zeca" });
        await ctx.SaveChangesAsync();

        var removidos = await service.RemoverOrfaosAsync();

        Assert.Equal(1, removidos);
        Assert.Equal("Rex", ctx.Agendamentos.Single().Pet);
    }

    [Fact]
    public void ObterDatasProximosBanhos_ListaDatasAjustadas()
    {
        using var ctx = TestHelpers.CreateContext();
        var service = TestHelpers.CreateAgendamentoService(ctx);

        var resultado = service.ObterDatasProximosBanhos("Mensal", "10,20");

        Assert.Equal(4, resultado.Count);
        Assert.All(resultado, r => Assert.NotEqual(DayOfWeek.Sunday, r.DataReal.DayOfWeek));
    }
}
