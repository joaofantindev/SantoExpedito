using Microsoft.Extensions.Caching.Memory;
using SantoExpedito.Services;

namespace SantoExpedito.Tests;

public class LoginAttemptServiceTests
{
    private static LoginAttemptService Create() => new(new MemoryCache(new MemoryCacheOptions()));

    [Fact]
    public void IsBlocked_NovoIp_NaoBloqueado()
    {
        var service = Create();
        Assert.False(service.IsBlocked("10.0.0.1"));
    }

    [Fact]
    public void IsBlocked_AposCincoFalhas_Bloqueia()
    {
        var service = Create();
        for (var i = 0; i < 5; i++)
            service.RecordFailedAttempt("10.0.0.2");

        Assert.True(service.IsBlocked("10.0.0.2"));
    }

    [Fact]
    public void Reset_LiberaBloqueio()
    {
        var service = Create();
        for (var i = 0; i < 5; i++)
            service.RecordFailedAttempt("10.0.0.3");

        service.Reset("10.0.0.3");

        Assert.False(service.IsBlocked("10.0.0.3"));
    }

    [Fact]
    public void IsBlocked_AbaixoDoLimite_NaoBloqueado()
    {
        var service = Create();
        for (var i = 0; i < 4; i++)
            service.RecordFailedAttempt("10.0.0.4");

        Assert.False(service.IsBlocked("10.0.0.4"));
    }
}
