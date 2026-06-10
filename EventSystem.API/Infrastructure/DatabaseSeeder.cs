using EventSystem.Core.Data;
using EventSystem.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace EventSystem.API.Infrastructure;

public static class DatabaseSeeder
{
      public static async Task SeedAsync(WebApplication app)
      {
            using var scope = app.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

            await context.Database.MigrateAsync();

            if (!await context.Roles.AnyAsync())
            {
                  context.Roles.AddRange(
                      new Role { Name = "Admin" },
                      new Role { Name = "Organizer" },
                      new Role { Name = "Student" }
                  );
                  await context.SaveChangesAsync();
                  logger.LogInformation("Seed : role zostały dodane");
            }

            if (!await context.Users.AnyAsync(u => u.Role.Name == "Admin"))
            {
                  var adminRole = await context.Roles.FirstAsync(r => r.Name == "Admin");

                  var adminEmail = config["AdminSettings:Email"]
                      ?? throw new InvalidOperationException("Brak AdminSettings:Email w konfiguracji");
                  var adminPassword = config["AdminSettings:Password"]
                      ?? throw new InvalidOperationException("Brak AdminSettings:Password w konfiguracji");

                  context.Users.Add(new User
                  {
                        FirstName = "System",
                        LastName = "Admin",
                        Email = adminEmail,
                        PasswordHash = BCrypt.Net.BCrypt.HashPassword(adminPassword),
                        RoleId = adminRole.Id
                  });

                  await context.SaveChangesAsync();
                  logger.LogInformation("Seed : konto administratora zostało utworzone ({Email})", adminEmail);
            }
      }
}