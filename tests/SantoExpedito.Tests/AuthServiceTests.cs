using System.Security.Claims;
using SantoExpedito.Data;
using SantoExpedito.Models;
using SantoExpedito.Services;

namespace SantoExpedito.Tests;

public class AuthServiceTests
{
    private static async Task<AppDbContextWithSeed> SeedAsync()
    {
        var ctx = TestHelpers.CreateContext();
        ctx.AdminUsers.AddRange(
            new AdminUser { Username = "casaderacao", PasswordHash = BCryptHelper.HashPassword("senha-admin"), Role = UserRole.Admin },
            new AdminUser { Username = "evin", PasswordHash = BCryptHelper.HashPassword("senha-root"), Role = UserRole.SuperAdmin });
        await ctx.SaveChangesAsync();
        return new AppDbContextWithSeed(ctx);
    }

    [Fact]
    public async Task ValidateUserAsync_CredenciaisValidas_RetornaTrue()
    {
        using var seed = await SeedAsync();
        var service = new AuthService(seed.Context);

        Assert.True(await service.ValidateUserAsync("evin", "senha-root"));
    }

    [Fact]
    public async Task ValidateUserAsync_SenhaInvalida_RetornaFalse()
    {
        using var seed = await SeedAsync();
        var service = new AuthService(seed.Context);

        Assert.False(await service.ValidateUserAsync("evin", "errada"));
    }

    [Fact]
    public async Task ValidateUserAsync_UsuarioInexistente_RetornaFalse()
    {
        using var seed = await SeedAsync();
        var service = new AuthService(seed.Context);

        Assert.False(await service.ValidateUserAsync("ninguem", "qualquer"));
    }

    [Fact]
    public async Task CreatePrincipalAsync_SuperAdmin_TemRoleSuperAdmin()
    {
        using var seed = await SeedAsync();
        var principal = await new AuthService(seed.Context).CreatePrincipalAsync("evin");

        Assert.True(principal.Identity!.IsAuthenticated);
        Assert.True(principal.IsInRole("SuperAdmin"));
        Assert.Equal("root", principal.FindFirst("UsuarioTipo")!.Value);
        Assert.Equal("evin", principal.FindFirst(ClaimTypes.Name)!.Value);
    }

    [Fact]
    public async Task CreatePrincipalAsync_Admin_TemRoleAdmin()
    {
        using var seed = await SeedAsync();
        var principal = await new AuthService(seed.Context).CreatePrincipalAsync("casaderacao");

        Assert.True(principal.IsInRole("admin"));
        Assert.Equal("evin", principal.FindFirst("UsuarioTipo")!.Value);
    }

    [Fact]
    public async Task CreatePrincipalAsync_UsuarioInexistente_NaoAutenticado()
    {
        using var seed = await SeedAsync();
        var principal = await new AuthService(seed.Context).CreatePrincipalAsync("ninguem");

        Assert.False(principal.Identity!.IsAuthenticated);
    }

    private sealed class AppDbContextWithSeed : IDisposable
    {
        public AppDbContext Context { get; }
        public AppDbContextWithSeed(AppDbContext context) => Context = context;
        public void Dispose() => Context.Dispose();
    }
}
