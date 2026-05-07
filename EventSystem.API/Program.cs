using EventSystem.Core.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// 1. Rejestracja usług
builder.Services.AddOpenApi();
builder.Services.AddControllers();

// Pobranie Connection String z zabezpieczeniem przed nullem
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

if (string.IsNullOrEmpty(connectionString))
{
    // Logika pomocnicza: jeśli nie znajdzie w Secrets/Environment, rzuci czytelnym błędem przy starcie
    throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
}

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));

// 2. Konfiguracja JWT
var jwtKey = builder.Configuration["JwtSettings:SecretKey"] ?? "TutajWpiszBardzoDlugiSekretnyKluczDoPoC";
var keyBytes = Encoding.UTF8.GetBytes(jwtKey);

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(keyBytes),
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
    });

// 3. Konfiguracja polityk dostępu
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("RequireOrganizerRole", policy => policy.RequireClaim("role", "Organizer"));
    options.AddPolicy("RequireAdminRole", policy => policy.RequireClaim("role", "Admin"));
});

var app = builder.Build();

// 4. Pipeline potoku żądań (Middleware)
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();