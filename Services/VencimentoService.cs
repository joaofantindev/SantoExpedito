using Microsoft.EntityFrameworkCore;
using SantoExpedito.Data;
using SantoExpedito.DTOs;
using SantoExpedito.Services.Interfaces;

namespace SantoExpedito.Services;

public class VencimentoService : IVencimentoService
{
    private readonly AppDbContext _context;

    public VencimentoService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<VencimentoListDto> GetVencimentosAsync()
    {
        var vencimentos = await _context.Vencimentos.ToListAsync();
        var hoje = DateTime.Now;
        var hojeStr = $"{hoje.Day:D2}/{hoje.Month:D2}/{hoje.Year}";
        var hojeInicio = new DateTime(hoje.Year, hoje.Month, hoje.Day);

        var todos = vencimentos.Select(v => MapToDto(v, hojeInicio)).ToList();

        var pagos = todos.Where(v => string.Equals(v.Status, "Pago", StringComparison.OrdinalIgnoreCase)).ToList();
        var pendentes = todos.Where(v => string.Equals(v.Status, "Pendente", StringComparison.OrdinalIgnoreCase)).ToList();
        var atrasados = todos.Where(v => string.Equals(v.Status, "Atrasado", StringComparison.OrdinalIgnoreCase)).ToList();
        var vencemHoje = todos.Where(v => v.VencimentoData == hojeStr).ToList();
        var vencemProx7 = todos.Where(v =>
        {
            if (string.IsNullOrEmpty(v.VencimentoData)) return false;
            var partes = v.VencimentoData.Split('/');
            if (partes.Length != 3) return false;
            if (DateTime.TryParse($"{partes[2]}-{partes[1]}-{partes[0]}", out var dt))
            {
                var diff = (dt - hojeInicio).Days;
                return diff >= 0 && diff <= 7 && !string.Equals(v.Status, "Pago", StringComparison.OrdinalIgnoreCase);
            }
            return false;
        }).ToList();

        return new VencimentoListDto
        {
            Todos = todos,
            Pagos = pagos,
            Pendentes = pendentes,
            Atrasados = atrasados,
            VencemHoje = vencemHoje,
            VencemProximos7Dias = vencemProx7
        };
    }

    public async Task<FinanceiroDto> GetFinanceiroAsync()
    {
        var clientes = await _context.Clientes.ToListAsync();

        return new FinanceiroDto
        {
            ClientesMensal = clientes.Count(c => c.Plano == "Mensal"),
            ClientesQuinzenal = clientes.Count(c => c.Plano == "Quinzenal"),
            ReceitaMensal = clientes.Where(c => c.Plano == "Mensal").Sum(c => c.Valor),
            ReceitaQuinzenal = clientes.Where(c => c.Plano == "Quinzenal").Sum(c => c.Valor),
            ReceitaTotal = clientes.Sum(c => c.Valor)
        };
    }

    public async Task<int> RemoverOrfaosAsync()
    {
        var nomesClientes = await _context.Clientes.Select(c => c.Nome).ToListAsync();
        var paraRemover = await _context.Vencimentos
            .Where(v => !nomesClientes.Contains(v.Cliente ?? ""))
            .ToListAsync();

        if (paraRemover.Count > 0)
        {
            _context.Vencimentos.RemoveRange(paraRemover);
            await _context.SaveChangesAsync();
        }
        return paraRemover.Count;
    }

    public async Task AlterarStatusAsync(int id, string status)
    {
        var vencimento = await _context.Vencimentos.FindAsync(id);
        if (vencimento != null)
        {
            vencimento.Status = status;
            await _context.SaveChangesAsync();
        }
    }

    public async Task RenovarPlanoAsync(int id)
    {
        var vencimento = await _context.Vencimentos.FindAsync(id);
        if (vencimento == null) return;

        var hoje = DateTime.Now;
        vencimento.DataInicio = $"{hoje.Day:D2}/{hoje.Month:D2}/{hoje.Year}";
        vencimento.VencimentoData = CalcularVencimento(hoje, vencimento.Plano);
        vencimento.Status = "Pago";

        var cliente = await _context.Clientes.FirstOrDefaultAsync(c => c.Nome == vencimento.Cliente);
        if (cliente != null)
        {
            cliente.BanhosRestantes = string.Equals(cliente.Plano, "Mensal", StringComparison.OrdinalIgnoreCase) ? 4 : 2;
        }

        await _context.SaveChangesAsync();
    }

    public async Task<VencimentoItemDto?> GetByIdAsync(int id)
    {
        var v = await _context.Vencimentos.FindAsync(id);
        if (v == null) return null;
        var hojeInicio = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day);
        return MapToDto(v, hojeInicio);
    }

    public async Task<VencimentoItemDto?> GetVencimentoClienteAsync(string cliente)
    {
        var v = await _context.Vencimentos
            .Where(v => v.Cliente == cliente)
            .OrderByDescending(v => v.Id)
            .FirstOrDefaultAsync();
        if (v == null) return null;
        var hojeInicio = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day);
        return MapToDto(v, hojeInicio);
    }

    private static VencimentoItemDto MapToDto(Models.Vencimento v, DateTime hojeInicio)
    {
        var status = v.Status;
        if (string.Equals(status, "Pago", StringComparison.OrdinalIgnoreCase))
            return new VencimentoItemDto
            {
                Id = v.Id, Cliente = v.Cliente, Plano = v.Plano,
                DataInicio = v.DataInicio, VencimentoData = v.VencimentoData,
                Valor = v.Valor, Status = status
            };

        if (!string.IsNullOrEmpty(v.VencimentoData))
        {
            var partes = v.VencimentoData.Split('/');
            if (partes.Length == 3 && DateTime.TryParse($"{partes[2]}-{partes[1]}-{partes[0]}", out var dt))
            {
                if (dt < hojeInicio)
                    status = "Atrasado";
            }
        }

        return new VencimentoItemDto
        {
            Id = v.Id, Cliente = v.Cliente, Plano = v.Plano,
            DataInicio = v.DataInicio, VencimentoData = v.VencimentoData,
            Valor = v.Valor, Status = status
        };
    }

    public static string CalcularVencimento(DateTime inicio, string? plano)
    {
        var dias = string.Equals(plano, "Quinzenal", StringComparison.OrdinalIgnoreCase) ? 15 : 30;
        var venc = inicio.AddDays(dias);
        return $"{venc.Day:D2}/{venc.Month:D2}/{venc.Year}";
    }
}
