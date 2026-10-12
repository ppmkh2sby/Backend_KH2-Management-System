using KH2.ManagementSystem.Domain.FaceRecognition;
using KH2.ManagementSystem.Domain.Kegiatans;
using KH2.ManagementSystem.Domain.Presensis;
using KH2.ManagementSystem.Domain.Santris;
using KH2.ManagementSystem.Domain.Sesis;
using KH2.ManagementSystem.Domain.Users;
using KH2.ManagementSystem.Infrastructure.Persistence;
using KH2.ManagementSystem.Infrastructure.FaceRecognition;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Xunit;

namespace KH2.ManagementSystem.UnitTests;

[Collection(PostgreSqlIntegrationFixtureDefinition.Name)]
public sealed class PostgreSqlCanonicalInvariantTests(PostgreSqlIntegrationFixture fixture)
{
    [Fact]
    public async Task CurrentModelCreatesCanonicalPgvectorSchema()
    {
        await using var context = await fixture.CreateContextAsync();
        await using var connection = new NpgsqlConnection(fixture.ConnectionString);
        await connection.OpenAsync();

        var profileEntity = context.Model.FindEntityType(typeof(FaceProfile));
        Assert.NotNull(profileEntity);
        Assert.Contains(
            profileEntity!.GetIndexes(),
            index => index.IsUnique
                && index.Properties.Count == 1
                && index.Properties[0].Name == nameof(FaceProfile.SantriId));

        Assert.Equal("vector(512)", await ScalarAsync(connection, """
            SELECT format_type(attribute.atttypid, attribute.atttypmod)
            FROM pg_attribute AS attribute
            JOIN pg_class AS table_class ON table_class.oid = attribute.attrelid
            WHERE table_class.relname = 'FaceEmbeddings' AND attribute.attname = 'Embedding';
            """));
        Assert.Equal("IX_FaceProfiles_SantriId", await ScalarAsync(connection, """
            SELECT indexname FROM pg_indexes
            WHERE tablename = 'FaceProfiles' AND indexname = 'IX_FaceProfiles_SantriId';
            """));
        Assert.Equal("UX_FaceEnrollments_Active_FaceProfile", await ScalarAsync(connection, """
            SELECT indexname FROM pg_indexes
            WHERE tablename = 'FaceEnrollments' AND indexname = 'UX_FaceEnrollments_Active_FaceProfile';
            """));
        Assert.Equal("UX_Presensis_SesiId_SantriId", await ScalarAsync(connection, """
            SELECT indexname FROM pg_indexes
            WHERE tablename = 'Presensis' AND indexname = 'UX_Presensis_SesiId_SantriId';
            """));
        Assert.Equal("CK_FaceEmbeddings_CaptureIndex_Range", await ScalarAsync(connection, """
            SELECT conname FROM pg_constraint
            WHERE conname = 'CK_FaceEmbeddings_CaptureIndex_Range';
            """));
        Assert.Equal("CK_FaceEmbeddings_QualityScore_Range", await ScalarAsync(connection, """
            SELECT conname FROM pg_constraint
            WHERE conname = 'CK_FaceEmbeddings_QualityScore_Range';
            """));
    }

    [Fact]
    public async Task DatabaseEnforcesOneFaceProfileAndOneActiveEnrollmentPerProfile()
    {
        await using var context = await fixture.CreateContextAsync();
        var santri = await SeedSantriAsync(context);
        var profile = new FaceProfile(Guid.NewGuid(), santri.Id, DateTimeOffset.UtcNow);
        context.FaceProfiles.Add(profile);
        await context.SaveChangesAsync();

        // A tracked one-to-one dependent is relationship-fixed-up by EF before SQL is
        // generated. Clear it so this assertion reaches PostgreSQL's unique index.
        context.ChangeTracker.Clear();
        context.FaceProfiles.Add(new FaceProfile(Guid.NewGuid(), santri.Id, DateTimeOffset.UtcNow));
        var profileViolation = await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
        Assert.Equal(PostgresErrorCodes.UniqueViolation, ((PostgresException)profileViolation.InnerException!).SqlState);

        context.ChangeTracker.Clear();
        var active = NewEnrollment(profile.Id);
        active.Activate(DateTimeOffset.UtcNow);
        context.FaceEnrollments.Add(active);
        await context.SaveChangesAsync();

        var pending = NewEnrollment(profile.Id);
        var superseded = NewEnrollment(profile.Id);
        superseded.Activate(DateTimeOffset.UtcNow);
        superseded.Supersede(DateTimeOffset.UtcNow);
        context.AddRange(pending, superseded);
        await context.SaveChangesAsync();

        var conflicting = NewEnrollment(profile.Id);
        conflicting.Activate(DateTimeOffset.UtcNow);
        context.FaceEnrollments.Add(conflicting);
        var enrollmentViolation = await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
        var postgres = Assert.IsType<PostgresException>(enrollmentViolation.InnerException);
        Assert.Equal(PostgresErrorCodes.UniqueViolation, postgres.SqlState);
        Assert.Equal("UX_FaceEnrollments_Active_FaceProfile", postgres.ConstraintName);
    }

    [Fact]
    public async Task DatabaseEnforcesEmbeddingCaptureAndQualityInvariants()
    {
        await using var context = await fixture.CreateContextAsync();
        var santri = await SeedSantriAsync(context);
        var profile = new FaceProfile(Guid.NewGuid(), santri.Id, DateTimeOffset.UtcNow);
        var enrollment = NewEnrollment(profile.Id);
        context.AddRange(profile, enrollment);
        await context.SaveChangesAsync();

        context.FaceEmbeddings.Add(NewEmbedding(enrollment.Id, 1, 0f));
        context.FaceEmbeddings.Add(NewEmbedding(enrollment.Id, 5, 1f));
        await context.SaveChangesAsync();

        context.FaceEmbeddings.Add(NewEmbedding(enrollment.Id, 1, null));
        var uniqueViolation = await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
        Assert.Equal(PostgresErrorCodes.UniqueViolation, ((PostgresException)uniqueViolation.InnerException!).SqlState);
    }

    [Fact]
    public async Task DatabaseEnforcesPresensiSesiSantriUniqueness()
    {
        await using var context = await fixture.CreateContextAsync();
        var santri = await SeedSantriAsync(context);
        var kegiatan = new Kegiatan(Guid.NewGuid(), "kajian", "malam", "test");
        var sesi = new Sesi(Guid.NewGuid(), kegiatan.Id, DateOnly.FromDateTime(DateTime.UtcNow), "test");
        context.AddRange(kegiatan, sesi);
        await context.SaveChangesAsync();

        context.Presensis.Add(NewPresensi(santri, kegiatan, sesi));
        await context.SaveChangesAsync();
        context.Presensis.Add(NewPresensi(santri, kegiatan, sesi));
        var violation = await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
        var postgres = Assert.IsType<PostgresException>(violation.InnerException);
        Assert.Equal(PostgresErrorCodes.UniqueViolation, postgres.SqlState);
        Assert.Equal("UX_Presensis_SesiId_SantriId", postgres.ConstraintName);
        Assert.True(AttendanceDuplicateViolationDetector.IsExpectedSesiSantriDuplicate(violation));
    }

    private static async Task<Santri> SeedSantriAsync(AppDbContext context)
    {
        var id = Guid.NewGuid();
        var user = new User(id, $"santri-{id:N}", "Integration Santri", null, UserRole.Santri, "hash");
        var santri = new Santri(Guid.NewGuid(), user.Id, "Integration Santri", id.ToString("N")[..12], "A", "B", "M", "A", "A");
        context.AddRange(user, santri);
        await context.SaveChangesAsync();
        return santri;
    }

    private static FaceEnrollment NewEnrollment(Guid profileId) =>
        new(Guid.NewGuid(), profileId, "arcface", "1", DateTimeOffset.UtcNow);

    private static FaceEmbedding NewEmbedding(Guid enrollmentId, int index, float? quality) =>
        new(Guid.NewGuid(), enrollmentId, Enumerable.Repeat(0.1f, FaceEmbedding.RequiredDimensions).ToArray(), index, quality);

    private static Presensi NewPresensi(Santri santri, Kegiatan kegiatan, Sesi sesi) =>
        new(Guid.NewGuid(), santri.Id, santri.FullName, "hadir", kegiatan.Id, sesi.Id, null, "malam");

    private static async Task<string?> ScalarAsync(NpgsqlConnection connection, string sql)
    {
        await using var command = new NpgsqlCommand(sql, connection);
        return (string?)await command.ExecuteScalarAsync();
    }
}
