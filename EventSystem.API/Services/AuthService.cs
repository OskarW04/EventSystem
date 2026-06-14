using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using EventSystem.API.DTOs;
using EventSystem.Core.Data;
using EventSystem.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace EventSystem.API.Services;

public class AuthService
{
    private readonly AppDbContext _context;
    private readonly IConfiguration _config;
    private readonly ILogger<AuthService> _logger;
    private readonly IEmailService _emailService;


    public AuthService(
    AppDbContext context,
    IConfiguration config,
    ILogger<AuthService> logger,
    IEmailService emailService)
    {
        _context = context;
        _config = config;
        _logger = logger;
        _emailService = emailService;
    }


    public async Task<bool> RegisterStudentAsync(RegisterStudentDto dto)
    {
        try
        {
            if (await _context.Users.AnyAsync(u => u.Email == dto.Email))
                return false;

            var role = await _context.Roles.FirstOrDefaultAsync(r => r.Name == "Student");
            if (role == null) return false;

            _context.Users.Add(new User
            {
                FirstName = dto.FirstName,
                LastName = dto.LastName,
                Email = dto.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                RoleId = role.Id
            });

            return await _context.SaveChangesAsync() > 0;
        }
        catch
        {
            return false;
        }
    }

    public async Task<string> RegisterOrganizerAsync(RegisterOrganizerDto dto)
    {
        var dbToken = await _context.OrganizationTokens
            .FirstOrDefaultAsync(t => t.TokenValue == dto.Token && !t.IsUsed);

        if (dbToken == null)
            return "Podany token jest nieprawidłowy lub został już użyty";

        if (await _context.Users.AnyAsync(u => u.Email == dto.Email))
            return "Podany adres e-mail jest już zajęty.";

        var role = await _context.Roles.FirstOrDefaultAsync(r => r.Name == "Organizer");

        await using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var user = new User
            {
                FirstName = dto.FirstName,
                LastName = dto.LastName,
                Email = dto.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                RoleId = role!.Id
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            dbToken.IsUsed = true;
            dbToken.UsedById = user.Id;
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return "Success";
        }
        catch
        {
            await transaction.RollbackAsync();
            return "Wystąpił błąd podczas tworzenia konta : spróbuj ponownie";
        }
    }

    public async Task<(AuthResponseDto? Response, string? Error)> LoginAsync(LoginDto dto)
    {
        var user = await _context.Users
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Email == dto.Email);

        if (user == null || !BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
            return (null, "Nieprawidłowy adres e-mail lub hasło");

        var accessToken = GenerateAccessToken(user);
        var refreshToken = await CreateRefreshTokenAsync(user.Id);

        return (new AuthResponseDto(accessToken, refreshToken.Token, user.Role.Name, user.Id), null);
    }

    public async Task<(AuthResponseDto? Response, string? Error)> RefreshAsync(string refreshTokenValue)
    {
        try
        {
            var storedToken = await _context.RefreshTokens
                .Include(rt => rt.User)
                    .ThenInclude(u => u.Role)
                .FirstOrDefaultAsync(rt => rt.Token == refreshTokenValue);

            if (storedToken == null || !storedToken.IsActive)
                return (null, "Token odświeżający jest nieważny lub wygasł : zaloguj się ponownie");

            storedToken.RevokedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            var newRefreshToken = await CreateRefreshTokenAsync(storedToken.UserId);
            var newAccessToken = GenerateAccessToken(storedToken.User);

            return (new AuthResponseDto(
                newAccessToken,
                newRefreshToken.Token,
                storedToken.User.Role.Name,
                storedToken.UserId), null);
        }
        catch
        {
            return (null, "Wystąpił błąd podczas odświeżania sesji");
        }
    }

    public async Task<bool> RevokeRefreshTokenAsync(string refreshTokenValue)
    {
        try
        {
            var storedToken = await _context.RefreshTokens
                .FirstOrDefaultAsync(rt => rt.Token == refreshTokenValue);

            if (storedToken == null || !storedToken.IsActive)
                return false;

            storedToken.RevokedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> InitiatePasswordResetAsync(string email)
    {
        try
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == email);

            if (user == null)
            {
                _logger.LogWarning("Password reset attempted for non-existent email: {Email}", email);
                return true;
            }

            var resetToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));

            user.PasswordResetToken = resetToken;
            user.PasswordResetTokenExpiry = DateTime.UtcNow.AddHours(1);
            await _context.SaveChangesAsync();

            await _emailService.SendPasswordResetEmailAsync(user.Email, resetToken);

            _logger.LogInformation("Password reset token generated for user: {Email}", email);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initiating password reset for {Email}", email);
            return false;
        }
    }

    public async Task<(bool Success, string? Error)> ResetPasswordAsync(ResetPasswordDto dto)
    {
        try
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == dto.Email);

            if (user == null ||
                user.PasswordResetToken == null ||
                user.PasswordResetToken != dto.ResetToken ||
                user.PasswordResetTokenExpiry == null ||
                user.PasswordResetTokenExpiry < DateTime.UtcNow)
                return (false, "Nieprawidłowy lub wygasły link resetowania hasła");

            if (dto.NewPassword.Length < 6)
                return (false, "Hasło musi mieć co najmniej 6 znaków");

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
            user.PasswordResetToken = null;
            user.PasswordResetTokenExpiry = null;

            var refreshTokens = await _context.RefreshTokens
                .Where(rt => rt.UserId == user.Id && rt.RevokedAt == null)
                .ToListAsync();

            foreach (var token in refreshTokens)
                token.RevokedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Password reset successful for user: {Email}", dto.Email);

            return (true, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resetting password for {Email}", dto.Email);
            return (false, "Wystąpił błąd podczas resetowania hasła");
        }
    }


    // ----- PRIVATE METHODS -----

    private string GenerateAccessToken(User user)
    {
        var keyBytes = Encoding.UTF8.GetBytes(_config["JwtSettings:SecretKey"]!);
        var expiryMinutes = int.Parse(_config["JwtSettings:AccessTokenExpiryMinutes"] ?? "15");

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role.Name)
            }),
            Expires = DateTime.UtcNow.AddMinutes(expiryMinutes),
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(keyBytes),
                SecurityAlgorithms.HmacSha256Signature)
        };

        var handler = new JwtSecurityTokenHandler();
        return handler.WriteToken(handler.CreateToken(tokenDescriptor));
    }

    private async Task<RefreshToken> CreateRefreshTokenAsync(int userId)
    {
        var expiryDays = int.Parse(_config["JwtSettings:RefreshTokenExpiryDays"] ?? "30");

        var refreshToken = new RefreshToken
        {
            Token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64)),
            ExpiresAt = DateTime.UtcNow.AddDays(expiryDays),
            UserId = userId
        };

        _context.RefreshTokens.Add(refreshToken);
        await _context.SaveChangesAsync();
        return refreshToken;
    }
}