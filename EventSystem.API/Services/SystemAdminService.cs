using EventSystem.Core.Data;
using EventSystem.Core.Entities;

namespace EventSystem.API.Services;

public class SystemAdminService
{
    private readonly AppDbContext _context;
    public SystemAdminService(AppDbContext context) => _context = context;

    public async Task<string> GenerateOrganizationTokenAsync(int adminId)
    {
        var tokenValue = Guid.NewGuid().ToString("N");
        var token = new OrganizationToken
        {
            TokenValue = tokenValue,
            CreatedById = adminId
        };
        _context.OrganizationTokens.Add(token);
        await _context.SaveChangesAsync();
        return tokenValue;
    }
}