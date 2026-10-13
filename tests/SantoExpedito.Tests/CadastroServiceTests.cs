using SantoExpedito.Data;
using SantoExpedito.DTOs;
using SantoExpedito.Services;

namespace SantoExpedito.Tests;

public class CadastroServiceTests
{
    private static CadastroService Create(AppDbContext ctx) =>
        new(ctx, TestHelpers.CreateAgendamentoService(ctx), TestHelpers.CreateCpfService());

    private static CadastroViewModel BuildModel()
    {
        return new CadastroViewModel
        {
            Responsavel = new ResponsavelViewModel
            {
                Nome = "Ana",
                Cpf = "111.222.333-44",
                Celular = "(11) 99999-0000",
                Endereco = "Rua 1",
                Email = "ana@email.com",
                Cep = "12345-000"
            },
            Pets = new List<PetViewModel>
            {
                new() { Nome = "Rex", Raca = "Poodle", Porte = "Pequeno" },
                new() { Nome = "Luna", Raca = "Pug", Porte = "Pequeno" }
            },
            Plano = "Mensal",
            Valor = 180m,
            DatasBanho = new List<string> { "10/06/2026", "20/06/2026" }
        };
    }

    [Fact]
    public async Task CreateClienteAsync_SemPets_RetornaErro()
    {
        using var ctx = TestHelpers.CreateContext();
        var model = BuildModel();
        model.Pets.Clear();

        var result = await Create(ctx).CreateClienteAsync(model);

        Assert.StartsWith("Adicione", result);
        Assert.Empty(ctx.Clientes);
    }

    [Fact]
    public async Task CreateClienteAsync_CriaClientePetVencimentoEAgenda()
    {
        using var ctx = TestHelpers.CreateContext();

        var result = await Create(ctx).CreateClienteAsync(BuildModel());

        Assert.Equal("Cliente cadastrado com sucesso!", result);
        Assert.Equal(2, ctx.Clientes.Count());
        Assert.Equal(2, ctx.Pets.Count());
        Assert.Single(ctx.Vencimentos);
        Assert.NotEmpty(ctx.Agendamentos);
        Assert.All(ctx.Clientes.ToList(), c => Assert.NotEqual("111.222.333-44", c.Cpf));
        Assert.Equal("Mensal", ctx.Vencimentos.Single().Plano);
    }
}
