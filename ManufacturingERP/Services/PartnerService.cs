using ManufacturingERP.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ManufacturingERP.Services;

public interface IPartnerService
{
    Task<List<Partner>> GetAllAsync();
    Task<Partner?> GetByIdAsync(int id);
    Task<bool> AddAsync(Partner partner);
    Task<bool> UpdateAsync(Partner partner);
    Task<bool> DeleteAsync(int id);
    Task<bool> IsCodeDuplicateAsync(string code, int excludeId = 0);
}

public class PartnerService : IPartnerService
{
    private readonly IDbContextFactory<ManufacturingContext> _contextFactory;

    public PartnerService(IDbContextFactory<ManufacturingContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<List<Partner>> GetAllAsync()
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        return await context.Partners.AsNoTracking().ToListAsync();
    }

    public async Task<Partner?> GetByIdAsync(int id)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        return await context.Partners.FindAsync(id);
    }

    public async Task<bool> AddAsync(Partner partner)
    {
        if (await IsCodeDuplicateAsync(partner.PartnerCode)) return false;
        using var context = await _contextFactory.CreateDbContextAsync();
        context.Partners.Add(partner);
        return await context.SaveChangesAsync() > 0;
    }

    public async Task<bool> UpdateAsync(Partner partner)
    {
        if (await IsCodeDuplicateAsync(partner.PartnerCode, partner.PartnerId)) return false;
        using var context = await _contextFactory.CreateDbContextAsync();
        context.Partners.Update(partner);
        return await context.SaveChangesAsync() > 0;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        var partner = await context.Partners.FindAsync(id);
        if (partner == null) return false;
        context.Partners.Remove(partner);
        return await context.SaveChangesAsync() > 0;
    }
    public async Task<bool> IsCodeDuplicateAsync(string code, int excludeId = 0)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        return await context.Partners.AnyAsync(p => p.PartnerCode == code && p.PartnerId != excludeId);
    }
}
