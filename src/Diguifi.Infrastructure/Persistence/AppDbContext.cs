using Diguifi.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Diguifi.Infrastructure.Persistence;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<WebhookEvent> WebhookEvents => Set<WebhookEvent>();
    public DbSet<Bundle> Bundles => Set<Bundle>();
    public DbSet<GameNotionPlayer> GameNotionPlayers => Set<GameNotionPlayer>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.Email).IsUnique();
            entity.HasIndex(x => x.GoogleSubject).IsUnique();
            entity.Property(x => x.Email).HasMaxLength(256);
            entity.Property(x => x.Name).HasMaxLength(256);
        });

        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.TokenHash).IsUnique();
        });

        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Price).HasColumnType("decimal(18,2)");
        });

        modelBuilder.Entity<Order>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Amount).HasColumnType("decimal(18,2)");
            entity.HasOne(x => x.User).WithMany(x => x.Orders).HasForeignKey(x => x.UserId);
            entity.HasOne(x => x.Product).WithMany(x => x.Orders).HasForeignKey(x => x.ProductId);
        });

        modelBuilder.Entity<WebhookEvent>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => new { x.Provider, x.ExternalEventId }).IsUnique();
        });

        modelBuilder.Entity<Bundle>(b =>
        {
            b.HasKey(x => x.ProductId);
            b.HasOne(x => x.Product)
             .WithOne()
             .HasForeignKey<Bundle>(x => x.ProductId);
        });

        modelBuilder.Entity<GameNotionPlayer>(e =>
        {
            e.HasKey(x => x.PlayerId);
            e.Property(x => x.PlayerId).HasMaxLength(100);
            e.HasOne(x => x.User)
             .WithMany()
             .HasForeignKey(x => x.UserId)
             .OnDelete(DeleteBehavior.SetNull);
        });
    }
}
