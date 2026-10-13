using SantoExpedito.Models;
using SantoExpedito.Services;

namespace SantoExpedito.Tests;

public class PetServiceTests
{
    [Fact]
    public async Task GetAllAsync_FiltraPorNomeTutorOuRaca()
    {
        using var ctx = TestHelpers.CreateContext();
        ctx.Pets.AddRange(
            new Pet { Nome = "Rex", Tutor = "Ana", Raca = "Poodle", Porte = "Pequeno" },
            new Pet { Nome = "Luna", Tutor = "Maria", Raca = "Pug", Porte = "Pequeno" });
        await ctx.SaveChangesAsync();

        var service = new PetService(ctx);

        Assert.Single(await service.GetAllAsync("rex"));
        Assert.Single(await service.GetAllAsync("maria"));
        Assert.Equal(2, (await service.GetAllAsync("pequeno")).Count);
    }

    [Fact]
    public async Task DeleteAsync_RemoveAgendamentosDoPet()
    {
        using var ctx = TestHelpers.CreateContext();
        ctx.Pets.Add(new Pet { Nome = "Rex", Tutor = "Ana" });
        ctx.Agendamentos.Add(new Agendamento { Pet = "Rex", Tutor = "Ana" });
        await ctx.SaveChangesAsync();
        var id = ctx.Pets.Single().Id;

        await new PetService(ctx).DeleteAsync(id);

        Assert.Empty(ctx.Pets);
        Assert.Empty(ctx.Agendamentos);
    }

    [Fact]
    public async Task GetByIdAsync_RetornaPet()
    {
        using var ctx = TestHelpers.CreateContext();
        ctx.Pets.Add(new Pet { Nome = "Rex", Tutor = "Ana" });
        await ctx.SaveChangesAsync();
        var id = ctx.Pets.Single().Id;

        var pet = await new PetService(ctx).GetByIdAsync(id);

        Assert.NotNull(pet);
        Assert.Equal("Rex", pet!.Nome);
    }
}
