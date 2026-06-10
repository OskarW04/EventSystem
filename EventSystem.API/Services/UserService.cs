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

    public async Task<UserProfileDto?> GetUserProfileAsync(int userId)
    {
        var user = await _context.Users
            .AsNoTracking()
            .Include(u => u.Role)
            .Include(u => u.SocialLinks)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null)
            return null;

        return new UserProfileDto(
            user.Id,
            user.FirstName,
            user.LastName,
            user.Email,
            user.Bio,
            user.Role.Name,
            user.CreatedAt,
            user.SocialLinks
                .Select(sl => new SocialLinkDto(sl.PlatformName, sl.Url))
                .ToList()
        );
    }

    public async Task<(bool Success, string? Error)> ChangePasswordAsync(
        int userId, ChangePasswordDto dto)
    {
        var user = await _context.Users.FindAsync(userId);

        if (user == null)
            return (false, "Użytkownik nie istnieje");

        if (!BCrypt.Net.BCrypt.Verify(dto.CurrentPassword, user.PasswordHash))
            return (false, "Obecne hasło jest nieprawidłowe");

        if (dto.NewPassword.Length < 6)
            return (false, "Nowe hasło musi mieć co najmniej 6 znaków");

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
        await _context.SaveChangesAsync();

        return (true, null);
    }

    public async Task<(bool Success, string? Error)> UpdateUserDetailsAsync(
        int userId, UpdateUserDetailsDto dto)
    {
        var user = await _context.Users.FindAsync(userId);

        if (user == null)
            return (false, "Użytkownik nie istnieje");

        if (dto.Email != user.Email)
        {
            var emailExists = await _context.Users
                .AnyAsync(u => u.Email == dto.Email && u.Id != userId);

            if (emailExists)
                return (false, "Podany adres e-mail jest już zajęty");
        }

        user.FirstName = dto.FirstName;
        user.LastName = dto.LastName;
        user.Email = dto.Email;

        await _context.SaveChangesAsync();
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> DeleteAccountAsync(int userId, string password)
    {
        var user = await _context.Users
            .Include(u => u.CreatedEvents)
                .ThenInclude(e => e.Tickets)
            .Include(u => u.Tickets)
            .Include(u => u.SocialLinks)
            .Include(u => u.RefreshTokens)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null)
            return (false, "Użytkownik nie istnieje");

        if (!BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
            return (false, "Nieprawidłowe hasło");

        if (user.CreatedEvents.Any(e => e.Date >= DateTime.UtcNow))
            return (false, "Nie można usunąć konta z aktywnymi wydarzeniami. Najpierw usuń lub przenieś swoje wydarzenia");

        _context.SocialLinks.RemoveRange(user.SocialLinks);
        _context.RefreshTokens.RemoveRange(user.RefreshTokens);

        foreach (var ev in user.CreatedEvents.Where(e => !string.IsNullOrEmpty(e.ImageUrl)))
        {
            var imagePath = Path.Combine(
                Directory.GetCurrentDirectory(), "wwwroot", ev.ImageUrl!.TrimStart('/'));

            if (File.Exists(imagePath))
                File.Delete(imagePath);
        }

        _context.Events.RemoveRange(user.CreatedEvents);
        _context.Users.Remove(user);

        await _context.SaveChangesAsync();
        return (true, null);
    }

}