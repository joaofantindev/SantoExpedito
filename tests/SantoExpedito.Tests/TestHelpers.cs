using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using SantoExpedito.Data;
using SantoExpedito.Services;
using SantoExpedito.Services.RegrasAgendamento;

namespace SantoExpedito.Tests;

internal static class TestHelpers
{
    public static AppDbContext CreateContext(string? name = null)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(name ?? Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    public static ICpfEncryptionService CreateCpfService()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["CpfEncryption:Key"] = "IFF5RR/C8cZTORhM5pECncLbXBH8TOGsiNE1t1Y+vNg=",
                ["CpfEncryption:IV"] = "/NnBNu9lUj8nTLJtQyCb5A=="
            })
            .Build();
        return new CpfEncryptionService(config);
    }

    public static AgendamentoService CreateAgendamentoService(AppDbContext context)
    {
        var regras = new ServicoRegrasAgendamento(new IRegraAgendamento[] { new RegraDomingo() });
        return new AgendamentoService(context, regras);
    }
}
