using System.Text.Json;
using KH2.ManagementSystem.Domain.FaceRecognition;
using KH2.ManagementSystem.Domain.Santris;
using KH2.ManagementSystem.Domain.Users;
using KH2.ManagementSystem.Infrastructure.Persistence.Configurations;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace KH2.ManagementSystem.UnitTests;

// Exercise the production relational configurations in an isolated SQLite database.
// Vector storage is replaced only here; PostgreSQL vector serialization needs a pgvector database.
public sealed class FaceFoundationRelationalTests
{
    [Fact]
    public void OneSantriCannotHaveMultipleFaceProfiles()
    {
        using var context = CreateContext();
        var santri = SeedSantri(context);
        context.Add(NewProfile(santri.Id));
        context.SaveChanges();
        context.ChangeTracker.Clear();
        context.Add(NewProfile(santri.Id));

        Assert.Throws<DbUpdateException>(() => context.SaveChanges());
    }

    [Fact]
    public void ProfileRequiresAnExistingSantri()
    {
        using var context = CreateContext();
        context.Add(NewProfile(Guid.NewGuid()));

        Assert.Throws<DbUpdateException>(() => context.SaveChanges());
    }

    [Fact]
    public void EmbeddingRequiresAnExistingFaceEnrollment()
    {
        using var context = CreateContext();
        context.Add(new FaceEmbedding(Guid.NewGuid(), Guid.NewGuid(), new float[512], 1));

        Assert.Throws<DbUpdateException>(() => context.SaveChanges());
    }

    [Fact]
    public void DeletingProfileCascadesUnloadedEmbeddings()
    {
        using var context = CreateContext();
        var santri = SeedSantri(context);
        var profile = NewProfile(santri.Id);
        var enrollment = NewEnrollment(profile.Id);
        context.Add(profile);
        context.Add(enrollment);
        context.Add(new FaceEmbedding(Guid.NewGuid(), enrollment.Id, new float[512], 1));
        context.SaveChanges();
        context.ChangeTracker.Clear();

        context.Remove(context.Set<FaceProfile>().Single());
        context.SaveChanges();

        Assert.Empty(context.Set<FaceEmbedding>());
        Assert.Single(context.Set<Santri>());
    }

    [Fact]
    public void DatabaseRejectsNonPositiveCaptureIndex()
    {
        using var context = CreateContext();
        var santri = SeedSantri(context);
        var profile = NewProfile(santri.Id);
        var enrollment = NewEnrollment(profile.Id);
        var embedding = new FaceEmbedding(Guid.NewGuid(), enrollment.Id, new float[512], 1);
        context.Add(profile);
        context.Add(enrollment);
        context.Add(embedding);
        context.Entry(embedding).Property(x => x.CaptureIndex).CurrentValue = 0;

        Assert.Throws<DbUpdateException>(() => context.SaveChanges());
    }

    private static FaceProfile NewProfile(Guid santriId) =>
        new(Guid.NewGuid(), santriId, "arcface", "1", DateTimeOffset.UtcNow);

    private static FaceEnrollment NewEnrollment(Guid profileId) =>
        new(Guid.NewGuid(), profileId, "arcface", "1", DateTimeOffset.UtcNow);

    [Fact]
    public void FailedEmbeddingInsertRollsBackTheWholeEnrollment()
    {
        using var context = CreateContext();
        var santri = SeedSantri(context);
        var profile = NewProfile(santri.Id);
        var enrollment = NewEnrollment(profile.Id);
        context.Add(profile);
        context.Add(enrollment);
        var embeddings = Enumerable.Range(1, 5)
            .Select(index => new FaceEmbedding(Guid.NewGuid(), enrollment.Id, new float[512], index)).ToArray();
        context.AddRange(embeddings);
        context.Entry(embeddings[4]).Property(x => x.CaptureIndex).CurrentValue = 0;

        Assert.Throws<DbUpdateException>(() => context.SaveChanges());
        context.ChangeTracker.Clear();
        Assert.Empty(context.Set<FaceProfile>());
        Assert.Empty(context.Set<FaceEmbedding>());
        Assert.Single(context.Set<Santri>());
    }

    private static Santri SeedSantri(DbContext context)
    {
        var user = new User(Guid.NewGuid(), "test-santri", "Test Santri", null, UserRole.Santri, "test-hash");
        var santri = new Santri(Guid.NewGuid(), user.Id, "Test Santri", "001", "A", "B", "M", "A", "A");
        context.AddRange(user, santri);
        context.SaveChanges();
        return santri;
    }

    private static FoundationContext CreateContext()
    {
        var connection = new SqliteConnection("Data Source=:memory:;Foreign Keys=True");
        connection.Open();
        var options = new DbContextOptionsBuilder<FoundationContext>()
            .UseSqlite(connection, contextOwnsConnection: true).Options;
        var context = new FoundationContext(options);
        context.Database.EnsureCreated();
        return context;
    }

    private sealed class FoundationContext(DbContextOptions<FoundationContext> options) : DbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfiguration(new UserConfiguration());
            modelBuilder.ApplyConfiguration(new SantriConfiguration());
            modelBuilder.ApplyConfiguration(new FaceProfileConfiguration());
            modelBuilder.ApplyConfiguration(new FaceEnrollmentConfiguration());
            modelBuilder.ApplyConfiguration(new FaceEmbeddingConfiguration());
            modelBuilder.Entity<FaceEmbedding>().Property<float[]>("Embedding")
                .HasConversion(
                    value => JsonSerializer.Serialize(value, (JsonSerializerOptions?)null),
                    value => JsonSerializer.Deserialize<float[]>(value, (JsonSerializerOptions?)null)!)
                .HasColumnType("TEXT");
        }
    }
}
