using KH2.ManagementSystem.Domain.Auth;
using KH2.ManagementSystem.Domain.Kafarahs;
using KH2.ManagementSystem.Domain.Kegiatans;
using KH2.ManagementSystem.Domain.LogKeluarMasuks;
using KH2.ManagementSystem.Domain.Presensis;
using KH2.ManagementSystem.Domain.ProgressKeilmuans;
using KH2.ManagementSystem.Domain.Santris;
using KH2.ManagementSystem.Domain.Sesis;
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
    public DbSet<Kegiatan> Kegiatans => Set<Kegiatan>();
    public DbSet<Sesi> Sesis => Set<Sesi>();
    public DbSet<Presensi> Presensis => Set<Presensi>();
    public DbSet<Kafarah> Kafarahs => Set<Kafarah>();
    public DbSet<ProgressKeilmuan> ProgressKeilmuans => Set<ProgressKeilmuan>();
    public DbSet<LogKeluarMasuk> LogKeluarMasuks => Set<LogKeluarMasuk>();
    public DbSet<WaliSantriRelation> WaliSantriRelations => Set<WaliSantriRelation>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<EmailVerificationCode> EmailVerificationCodes => Set<EmailVerificationCode>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
