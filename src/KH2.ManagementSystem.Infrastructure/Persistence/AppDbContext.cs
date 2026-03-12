using KH2.ManagementSystem.Domain.Auth;
using KH2.ManagementSystem.Domain.Santris;
using KH2.ManagementSystem.Domain.Users;
using KH2.ManagementSystem.Domain.Walis;
using Microsoft.EntityFrameworkCore;

namespace KH2.ManagementSystem.Infrastructure.Persistence;

public sealed class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Santri> Santris => Set<Santri>();
    public DbSet<WaliSantriRelation> WaliSantriRelations => Set<WaliSantriRelation>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}