using EventSystem.API.Services;
using EventSystem.Core.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// 1. Konfiguracja bazy danych
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// 2. Rejestracja serwisów logiki biznesowej
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<SystemAdminService>();
builder.Services.AddScoped<EventService>();
builder.Services.AddScoped<TicketService>();
builder.Services.AddScoped<UserService>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// 3. Konfiguracja CORS (niezbędna dla ciasteczek)
builder.Services.AddCors(options =>
{
    options.AddPolicy("FrontendClient", policy =>
    {
        policy.WithOrigins("http://localhost:3000") // Możesz to zmienić na środowisku produkcyjnym
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

// 4. Konfiguracja JWT i autoryzacji opartej o Cookies
// Klucz musi mieć co najmniej 32 znaki dla algorytmu HMAC-SHA256
var jwtKey = builder.Configuration["JwtSettings:SecretKey"] ?? "ToJestBardzoTajnyKluczZastepczyMinimum32Znaki";
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

        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                // Odczyt tokenu JWT bezpośrednio z ciasteczka
                if (context.Request.Cookies.ContainsKey("X-Access-Token"))
                {
                    context.Token = context.Request.Cookies["X-Access-Token"];
                }
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    if (!context.Roles.Any())
    {
        context.Roles.AddRange(
            new EventSystem.Core.Entities.Role { Name = "Admin" },
            new EventSystem.Core.Entities.Role { Name = "Organizer" },
            new EventSystem.Core.Entities.Role { Name = "Student" }
        );
        context.SaveChanges();
    }

    if (!context.Users.Any(u => u.Role.Name == "Admin"))
    {
        var adminRole = context.Roles.First(r => r.Name == "Admin");
        var adminEmail = app.Configuration["AdminSettings:Email"];
        var adminPassword = app.Configuration["AdminSettings:Password"];

        var adminUser = new EventSystem.Core.Entities.User
        {
            FirstName = "System",
            LastName = "Admin",
            Email = adminEmail,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(adminPassword),
            RoleId = adminRole.Id
        };

        context.Users.Add(adminUser);
        context.SaveChanges();
    }
}

// 5. Potok żądań HTTP
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Pozwala na serwowanie zdjęć wydarzeń (katalog wwwroot)
app.UseStaticFiles();

// CORS musi być przed weryfikacją autentykacji
app.UseCors("FrontendClient");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();