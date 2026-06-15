using EventSystem.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace EventSystem.Core.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users { get; set; }
    public DbSet<Role> Roles { get; set; }
    public DbSet<SocialLink> SocialLinks { get; set; }
    public DbSet<OrganizationToken> OrganizationTokens { get; set; }
    public DbSet<Event> Events { get; set; }
    public DbSet<Ticket> Tickets { get; set; }
    public DbSet<EventView> EventViews { get; set; }
    public DbSet<EventPresave> EventPresaves { get; set; }
    public DbSet<RefreshToken> RefreshTokens { get; set; }
    public DbSet<AuditLog> AuditLogs { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // index : User.Email
        // authentication
        modelBuilder.Entity<User>()
            .HasIndex(u => u.Email)
            .IsUnique();

        // index : Role.Name
        // authentication and role checks
        modelBuilder.Entity<Role>()
            .HasIndex(r => r.Name)
            .IsUnique();

        // index : OrganizationToken.TokenValue
        // event creation
        modelBuilder.Entity<OrganizationToken>()
            .HasIndex(ot => ot.TokenValue)
            .IsUnique();

        // index : Ticket.EventId + Ticket.StudentId
        // ensuring one ticket per student per event
        modelBuilder.Entity<Ticket>()
            .HasIndex(t => new { t.EventId, t.StudentId })
            .IsUnique();

        // index : Ticket.ScanToken
        // scanning
        modelBuilder.Entity<Ticket>()
            .HasIndex(t => t.ScanToken)
            .IsUnique();

        // index : EventView.EventId + EventView.CreatedAt
        // szybkie liczenie clicks24h (odsłony z ostatnich 24h)
        modelBuilder.Entity<EventView>()
            .HasIndex(v => new { v.EventId, v.CreatedAt });

        // index : EventPresave.EventId + EventPresave.StudentId
        // jeden pre-save per student per wydarzenie
        modelBuilder.Entity<EventPresave>()
            .HasIndex(p => new { p.EventId, p.StudentId })
            .IsUnique();

        // index : RefreshToken.Token
        // authentication
        modelBuilder.Entity<RefreshToken>()
            .HasIndex(rt => rt.Token)
            .IsUnique();

        // index : AuditLog.CreatedAt
        // admin logs browsing
        modelBuilder.Entity<AuditLog>()
            .HasIndex(al => al.CreatedAt);

        // ROUTING
        // Event
        modelBuilder.Entity<Event>()
            .HasOne(e => e.Organizer)
            .WithMany(u => u.CreatedEvents)
            .HasForeignKey(e => e.OrganizerId)
            .OnDelete(DeleteBehavior.Restrict);

        // Ticket
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

        // EventView - kasowane razem z wydarzeniem
        modelBuilder.Entity<EventView>()
            .HasOne(v => v.Event)
            .WithMany(e => e.Views)
            .HasForeignKey(v => v.EventId)
            .OnDelete(DeleteBehavior.Cascade);

        // EventPresave - kasowane razem z wydarzeniem; po stronie studenta Restrict
        // (czyszczone ręcznie przy usuwaniu użytkownika, jak Tickets)
        modelBuilder.Entity<EventPresave>()
            .HasOne(p => p.Event)
            .WithMany(e => e.Presaves)
            .HasForeignKey(p => p.EventId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<EventPresave>()
            .HasOne(p => p.Student)
            .WithMany(u => u.Presaves)
            .HasForeignKey(p => p.StudentId)
            .OnDelete(DeleteBehavior.Restrict);

        // OrganizationToken
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

        // SocialLink
        modelBuilder.Entity<SocialLink>()
            .HasOne(sl => sl.User)
            .WithMany(u => u.SocialLinks)
            .HasForeignKey(sl => sl.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // RefreshToken
        modelBuilder.Entity<RefreshToken>()
            .HasOne(rt => rt.User)
            .WithMany(u => u.RefreshTokens)
            .HasForeignKey(rt => rt.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // AuditLog
        modelBuilder.Entity<AuditLog>()
            .HasOne(al => al.User)
            .WithMany()
            .HasForeignKey(al => al.UserId)
            .OnDelete(DeleteBehavior.Restrict);

    }
}