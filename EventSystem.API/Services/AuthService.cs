using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
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
    private readonly IHttpContextAccessor _httpContextAccessor; // Dodane dla ciasteczek

    public AuthService(AppDbContext context, IConfiguration config, IHttpContextAccessor httpContextAccessor)
    {
        _context = context;
        _config = config;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<bool> RegisterStudentAsync(RegisterStudentDto dto)
    {
        if (await _context.Users.AnyAsync(u => u.Email == dto.Email)) return false;
        var role = await _context.Roles.FirstOrDefaultAsync(r => r.Name == "Student");
        if (role == null) return false;

        var user = new User
        {
            FirstName = dto.FirstName,
            LastName = dto.LastName,
            Email = dto.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
            RoleId = role.Id
        };
        _context.Users.Add(user);
        return await _context.SaveChangesAsync() > 0;
    }

    public async Task<string> RegisterOrganizerAsync(RegisterOrganizerDto dto)
    {
        var dbToken = await _context.OrganizationTokens.FirstOrDefaultAsync(t => t.TokenValue == dto.Token && !t.IsUsed);
        if (dbToken == null) return "Nieprawidłowy lub zużyty token.";
        if (await _context.Users.AnyAsync(u => u.Email == dto.Email)) return "Email zajęty.";
        var role = await _context.Roles.FirstOrDefaultAsync(r => r.Name == "Organizer");

        using var transaction = await _context.Database.BeginTransactionAsync();
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
            return "Błąd bazy danych.";
        }
    }

    public async Task<(string? Token, string? Error)> LoginAsync(LoginDto dto)
    {
        var user = await _context.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.Email == dto.Email);
        if (user == null || !BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash)) 
            return (null, "Błędny email lub hasło.");

        var keyBytes = Encoding.UTF8.GetBytes(_config["JwtSettings:SecretKey"]!);
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role.Name)
            }),
            Expires = DateTime.UtcNow.AddDays(7),
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(keyBytes), SecurityAlgorithms.HmacSha256Signature)
        };
        
        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.WriteToken(tokenHandler.CreateToken(tokenDescriptor));

        // --- KLUCZ: Ustawianie ciasteczka ---
        var cookieOptions = new CookieOptions
        {
            HttpOnly = true,
            Secure = false, // Musi być false dla HTTP / IP
            SameSite = SameSiteMode.Lax,
            Expires = DateTime.UtcNow.AddDays(7)
        };

        _httpContextAccessor.HttpContext?.Response.Cookies.Append("X-Access-Token", token, cookieOptions);

        return (token, null);
    }
}