using SantoExpedito.Services.RegrasAgendamento;

namespace SantoExpedito.Tests;

public class RegrasAgendamentoTests
{
    [Fact]
    public void RegraDomingo_MoveDomingoParaSegunda()
    {
        var domingo = new DateTime(2026, 6, 7);
        Assert.Equal(DayOfWeek.Sunday, domingo.DayOfWeek);

        var resultado = new RegraDomingo().Aplicar(domingo);

        Assert.Equal(new DateTime(2026, 6, 8), resultado);
    }

    [Theory]
    [InlineData("2026-06-08")]
    [InlineData("2026-06-09")]
    [InlineData("2026-06-13")]
    public void RegraDomingo_NaoAlteraOutrosDias(string iso)
    {
        var data = DateTime.Parse(iso);
        Assert.Equal(data, new RegraDomingo().Aplicar(data));
    }

    [Fact]
    public void ServicoRegras_AplicaRegrasEmSequencia()
    {
        var servico = new ServicoRegrasAgendamento(new IRegraAgendamento[]
        {
            new RegraDomingo(),
            new AdicionaUmDia()
        });

        var domingo = new DateTime(2026, 6, 7);

        Assert.Equal(new DateTime(2026, 6, 9), servico.Aplicar(domingo));
    }

    private sealed class AdicionaUmDia : IRegraAgendamento
    {
        public string Nome => "Adiciona 1 dia";
        public DateTime Aplicar(DateTime data) => data.AddDays(1);
    }
}
