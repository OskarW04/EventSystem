using EventSystem.API.DTOs;
using EventSystem.Core.Data;
using EventSystem.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace EventSystem.API.Services;

public class UserService
{
    private readonly AppDbContext _context;

    public UserService(AppDbContext context) => _context = context;

    public async Task<bool> UpdateProfileAsync(int userId, UpdateProfileDto dto)
    {
        var user = await _context.Users
            .Include(u => u.SocialLinks)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null)
            return false;

        user.Bio = dto.Bio;

        _context.SocialLinks.RemoveRange(user.SocialLinks);

        _context.SocialLinks.AddRange(
            dto.SocialLinks.Take(5).Select(l => new SocialLink
            {
                PlatformName = l.PlatformName,
                Url = l.Url,
                UserId = userId
            })
        );

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<PublicProfileDto?> GetPublicProfileAsync(int userId)
    {
        var user = await _context.Users
            .AsNoTracking()
            .Include(u => u.SocialLinks)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null)
            return null;

        return new PublicProfileDto(
            user.FirstName,
            user.LastName,
            user.Bio,
            user.SocialLinks
                .Select(sl => new SocialLinkDto(sl.PlatformName, sl.Url))
                .ToList()
        );
    }
}