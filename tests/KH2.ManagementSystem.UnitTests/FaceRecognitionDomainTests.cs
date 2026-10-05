using KH2.ManagementSystem.Domain.FaceRecognition;
using KH2.ManagementSystem.Domain.Santris;
using KH2.ManagementSystem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Pgvector.EntityFrameworkCore;
using Xunit;

namespace KH2.ManagementSystem.UnitTests;

public sealed class FaceRecognitionDomainTests
{
    [Fact]
    public void FaceProfileStatusTransitionsAreApplied()
    {
        var now = DateTimeOffset.UtcNow;
        var profile = new FaceProfile(Guid.NewGuid(), Guid.NewGuid(), "arcface", "1.0", now);

        Assert.Equal(FaceProfileStatus.Pending, profile.Status);

        profile.Activate(now.AddMinutes(1));
        profile.MarkVerified(now.AddMinutes(2));

        Assert.Equal(FaceProfileStatus.Active, profile.Status);
        Assert.Equal(now.AddMinutes(2), profile.LastVerifiedAtUtc);

        profile.UpdateModelVersion("2.0", now.AddMinutes(3));
        Assert.Equal(FaceProfileStatus.NeedsReEnrollment, profile.Status);

        profile.Disable(now.AddMinutes(4));
        Assert.Equal(FaceProfileStatus.Disabled, profile.Status);
    }

    [Fact]
    public void FaceEmbeddingRejectsInvalidCaptureIndex()
    {
        var embedding = new float[FaceEmbedding.RequiredDimensions];

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new FaceEmbedding(Guid.NewGuid(), Guid.NewGuid(), embedding, 0));
    }

    [Fact]
    public void FaceEmbeddingRejectsWrongDimensions()
    {
        Assert.Throws<ArgumentException>(() =>
            new FaceEmbedding(Guid.NewGuid(), Guid.NewGuid(), new float[511], 1));
    }

    [Fact]
    public void EmptyOwnerIdentifiersAreRejected()
    {
        Assert.Throws<ArgumentException>(() =>
            new FaceProfile(Guid.NewGuid(), Guid.Empty, "arcface", "1", DateTimeOffset.UtcNow));
        Assert.Throws<ArgumentException>(() =>
            new FaceEmbedding(Guid.NewGuid(), Guid.Empty, new float[512], 1));
    }

    [Fact]
    public void InactiveProfileCannotBeVerified()
    {
        var now = DateTimeOffset.UtcNow;
        var profile = new FaceProfile(Guid.NewGuid(), Guid.NewGuid(), "arcface", "1", now);
        Assert.Throws<InvalidOperationException>(() => profile.MarkVerified(now));
        profile.Activate(now);
        profile.RequireReEnrollment(now);
        Assert.Equal(FaceProfileStatus.NeedsReEnrollment, profile.Status);
        Assert.Throws<InvalidOperationException>(() => profile.MarkVerified(now));
    }

    [Fact]
    public void FaceProfileHasUniqueRequiredSantriRelationship()
    {
        using var context = CreateContext();
        var entityType = context.Model.FindEntityType(typeof(FaceProfile))!;
        var santriProperty = entityType.FindProperty(nameof(FaceProfile.SantriId))!;
        var foreignKey = entityType.GetForeignKeys().Single(x => x.PrincipalEntityType.ClrType == typeof(Santri));

        Assert.False(santriProperty.IsNullable);
        Assert.True(entityType.GetIndexes().Single(x => x.Properties.SequenceEqual([santriProperty])).IsUnique);
        Assert.Equal(DeleteBehavior.Cascade, foreignKey.DeleteBehavior);
    }

    [Fact]
    public void FaceEmbeddingBelongsToFaceEnrollmentAndCascadesOnDelete()
    {
        using var context = CreateContext();
        var entityType = context.Model.FindEntityType(typeof(FaceEmbedding))!;
        var foreignKey = entityType.GetForeignKeys().Single();

        Assert.Equal(typeof(FaceEnrollment), foreignKey.PrincipalEntityType.ClrType);
        Assert.True(foreignKey.IsRequired);
        Assert.Equal(DeleteBehavior.Cascade, foreignKey.DeleteBehavior);
        Assert.Contains(entityType.GetIndexes(), index =>
            index.Properties.Single().Name == nameof(FaceEmbedding.FaceEnrollmentId));
    }

    [Fact]
    public void EnrollmentRegisterRequiresItsCompleteTransition()
    {
        var enrollment = new FaceEnrollment(Guid.NewGuid(), Guid.NewGuid());
        var now = DateTimeOffset.UtcNow;

        enrollment.SetCaptureCount(5, now);
        enrollment.Register(now);

        Assert.Equal(FaceEnrollmentStatus.Registered, enrollment.Status);
        Assert.Equal(5, enrollment.CaptureCount);
        Assert.Equal(now, enrollment.EmbeddingUpdatedAtUtc);
    }

    [Fact]
    public void ClosedSessionCannotBeReopened()
    {
        var session = new FaceAttendanceSession(
            Guid.NewGuid(), "A", "Kajian", "malam", DateOnly.FromDateTime(DateTime.UtcNow), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        var now = DateTimeOffset.UtcNow;
        session.Open(now);
        session.Close(now);

        Assert.Throws<InvalidOperationException>(() => session.Open(now));
    }

    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(
                "Host=localhost;Database=kh2_model_tests;Username=postgres;Password=postgres",
                npgsqlOptions => npgsqlOptions.UseVector())
            .Options;

        return new AppDbContext(options);
    }
}
