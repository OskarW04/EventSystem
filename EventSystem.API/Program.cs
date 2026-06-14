using EventSystem.API.Infrastructure;
using EventSystem.API.Services;
using EventSystem.Core.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<SystemAdminService>();
builder.Services.AddScoped<EventService>();
builder.Services.AddScoped<TicketService>();
builder.Services.AddScoped<UserService>();

builder.Services.AddControllers();

builder.Services.AddCors(options =>
{
    options.AddPolicy("FrontendClient", policy =>
    {
        policy
            .WithOrigins(
                "http://localhost:3000",
                "https://eventsystem.oskarwitek.com")
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials();
    });
});

var jwtKey = builder.Configuration["JwtSettings:SecretKey"]
    ?? throw new InvalidOperationException("Brak JwtSettings:SecretKey w konfiguracji.");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };

        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                if (context.Request.Cookies.ContainsKey("X-Access-Token"))
                    context.Token = context.Request.Cookies["X-Access-Token"];

                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}

await DatabaseSeeder.SeedAsync(app);

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseCors("FrontendClient");

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();