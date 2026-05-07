using EventSystem.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace EventSystem.Core.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users { get; set; }
    public DbSet<Role> Roles { get; set; }
    public DbSet<OrganizationToken> OrganizationTokens { get; set; }
    public DbSet<Event> Events { get; set; }
    public DbSet<Ticket> Tickets { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>().HasIndex(u => u.Email).IsUnique();
        modelBuilder.Entity<Role>().HasIndex(r => r.Name).IsUnique();
        modelBuilder.Entity<OrganizationToken>().HasIndex(ot => ot.TokenValue).IsUnique();

        modelBuilder.Entity<Event>()
            .HasOne(e => e.Organizer)
            .WithMany(u => u.CreatedEvents)
            .HasForeignKey(e => e.OrganizerId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Ticket>()
            .HasOne(t => t.Event)
            .WithMany(e => e.Tickets)
            .HasForeignKey(t => t.EventId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Ticket>()
            .HasOne(t => t.Student)
            .WithMany(u => u.Tickets)
            .HasForeignKey(t => t.StudentId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<OrganizationToken>()
            .HasOne(ot => ot.CreatedBy)
            .WithMany(u => u.CreatedTokens)
            .HasForeignKey(ot => ot.CreatedById)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<OrganizationToken>()
            .HasOne(ot => ot.UsedBy)
            .WithOne()
            .HasForeignKey<OrganizationToken>(ot => ot.UsedById)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
