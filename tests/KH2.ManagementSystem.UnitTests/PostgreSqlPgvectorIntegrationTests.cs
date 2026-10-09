using KH2.ManagementSystem.Application.Abstractions.FaceRecognition;
using KH2.ManagementSystem.Domain.FaceRecognition;
using KH2.ManagementSystem.Domain.Santris;
using KH2.ManagementSystem.Domain.Users;
using KH2.ManagementSystem.Infrastructure.FaceRecognition;
using KH2.ManagementSystem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Xunit;

namespace KH2.ManagementSystem.UnitTests;

[Collection(PostgreSqlIntegrationFixtureDefinition.Name)]
public sealed class PostgreSqlPgvectorIntegrationTests(PostgreSqlIntegrationFixture fixture)
{
    [Fact]
    public async Task PgvectorStores512DimensionsAndRejectsOtherDimensions()
    {
        await using var context = await fixture.CreateContextAsync();
        var (_, profile) = await AddActiveProfileAsync(context, "vector-owner");
        var enrollment = NewActiveEnrollment(profile.Id, "arcface", "1");
        context.FaceEnrollments.Add(enrollment);
        context.FaceEmbeddings.Add(NewEmbedding(enrollment.Id, UnitVector(0), 1));
        await context.SaveChangesAsync();

        await using var connection = new NpgsqlConnection(fixture.ConnectionString);
        await connection.OpenAsync();
        Assert.Equal("vector(512)", await ScalarAsync(connection, """
            SELECT format_type(attribute.atttypid, attribute.atttypmod)
            FROM pg_attribute AS attribute
            JOIN pg_class AS table_class ON table_class.oid = attribute.attrelid
            WHERE table_class.relname = 'FaceEmbeddings' AND attribute.attname = 'Embedding';
            """));

        var distance = await ScalarDoubleAsync(connection, """
            SELECT @left::vector <=> @right::vector;
            """, ToVectorLiteral(UnitVector(0)), ToVectorLiteral(UnitVector(0)));
        Assert.Equal(0d, distance, 6);
        Assert.Equal(1d, 1d - distance, 6);

        await AssertInvalidDimensionAsync(connection, enrollment.Id, 511);
        await AssertInvalidDimensionAsync(connection, enrollment.Id, 513);
    }

    [Fact]
    public async Task FaceMatchReaderUsesRealPgvectorAndReturnsDistinctEligibleIdentities()
    {
        await using var context = await fixture.CreateContextAsync();
        var (firstSantri, firstProfile) = await AddActiveProfileAsync(context, "first");
        var (secondSantri, secondProfile) = await AddActiveProfileAsync(context, "second");
        var (_, disabledProfile) = await AddActiveProfileAsync(context, "disabled");
        disabledProfile.Disable(DateTimeOffset.UtcNow);

        var firstEnrollment = NewActiveEnrollment(firstProfile.Id, "arcface", "1");
        var secondEnrollment = NewActiveEnrollment(secondProfile.Id, "arcface", "1");
        var disabledEnrollment = NewActiveEnrollment(disabledProfile.Id, "arcface", "1");
        context.AddRange(firstEnrollment, secondEnrollment, disabledEnrollment);
        context.AddRange(
            NewEmbedding(firstEnrollment.Id, UnitVector(0), 1),
            NewEmbedding(firstEnrollment.Id, Normalized(0.99f, 0.1f), 2),
            NewEmbedding(secondEnrollment.Id, Normalized(0.8f, 0.6f), 1),
            NewEmbedding(disabledEnrollment.Id, UnitVector(0), 1));
        await context.SaveChangesAsync();

        var reader = new FaceMatchReader(context);
        var candidates = await reader.FindNearestAsync(
            new FaceEmbeddingResult(UnitVector(0), "arcface", "1"),
            CancellationToken.None);

        Assert.Collection(candidates,
            candidate =>
            {
                Assert.Equal(firstSantri.Id, candidate.SantriId);
                Assert.Equal(1d, candidate.Similarity, 5);
            },
            candidate => Assert.Equal(secondSantri.Id, candidate.SantriId));
    }

    [Fact]
    public async Task FaceMatchReaderIncludesOnlyTheCanonicalEligibleProfileEnrollmentModelAndAccount()
    {
        await using var context = await fixture.CreateContextAsync();
        var eligible = await AddCandidateAsync(context, "eligible", FaceProfileStatus.Active, FaceEnrollmentStatus.Active, "arcface", "1", UserRole.Santri, true);
        await AddCandidateAsync(context, "pending-profile", FaceProfileStatus.Pending, FaceEnrollmentStatus.Active, "arcface", "1", UserRole.Santri, true);
        await AddCandidateAsync(context, "disabled-profile", FaceProfileStatus.Disabled, FaceEnrollmentStatus.Active, "arcface", "1", UserRole.Santri, true);
        await AddCandidateAsync(context, "needs-profile", FaceProfileStatus.NeedsReEnrollment, FaceEnrollmentStatus.Active, "arcface", "1", UserRole.Santri, true);
        await AddCandidateAsync(context, "pending-enrollment", FaceProfileStatus.Active, FaceEnrollmentStatus.Pending, "arcface", "1", UserRole.Santri, true);
        await AddCandidateAsync(context, "superseded", FaceProfileStatus.Active, FaceEnrollmentStatus.Superseded, "arcface", "1", UserRole.Santri, true);
        await AddCandidateAsync(context, "failed", FaceProfileStatus.Active, FaceEnrollmentStatus.Failed, "arcface", "1", UserRole.Santri, true);
        await AddCandidateAsync(context, "model-name", FaceProfileStatus.Active, FaceEnrollmentStatus.Active, "other", "1", UserRole.Santri, true);
        await AddCandidateAsync(context, "model-version", FaceProfileStatus.Active, FaceEnrollmentStatus.Active, "arcface", "other", UserRole.Santri, true);
        await AddCandidateAsync(context, "inactive", FaceProfileStatus.Active, FaceEnrollmentStatus.Active, "arcface", "1", UserRole.Santri, false);
        await AddCandidateAsync(context, "non-santri", FaceProfileStatus.Active, FaceEnrollmentStatus.Active, "arcface", "1", UserRole.Admin, true);

        var candidates = await new FaceMatchReader(context).FindNearestAsync(new FaceEmbeddingResult(UnitVector(0), "arcface", "1"), CancellationToken.None);
        var candidate = Assert.Single(candidates);
        Assert.Equal(eligible.Id, candidate.SantriId);
    }

    private static async Task<Santri> AddCandidateAsync(AppDbContext context, string label, FaceProfileStatus profileStatus,
        FaceEnrollmentStatus enrollmentStatus, string modelName, string modelVersion, UserRole role, bool isActive)
    {
        var id = Guid.NewGuid();
        var user = new User(id, $"{label}-{id:N}", label, null, role, "hash", isActive: isActive);
        var santri = new Santri(Guid.NewGuid(), user.Id, label, id.ToString("N")[..12], "A", "B", "M", "A", "A");
        var profile = new FaceProfile(Guid.NewGuid(), santri.Id, DateTimeOffset.UtcNow);
        if (profileStatus == FaceProfileStatus.Active) profile.ActivateProfile(DateTimeOffset.UtcNow);
        else if (profileStatus == FaceProfileStatus.Disabled) profile.Disable(DateTimeOffset.UtcNow);
        else if (profileStatus == FaceProfileStatus.NeedsReEnrollment) profile.RequireReEnrollment(DateTimeOffset.UtcNow);
        var enrollment = new FaceEnrollment(Guid.NewGuid(), profile.Id, modelName, modelVersion, DateTimeOffset.UtcNow);
        if (enrollmentStatus == FaceEnrollmentStatus.Active) enrollment.Activate(DateTimeOffset.UtcNow);
        else if (enrollmentStatus == FaceEnrollmentStatus.Superseded) { enrollment.Activate(DateTimeOffset.UtcNow); enrollment.Supersede(DateTimeOffset.UtcNow); }
        else if (enrollmentStatus == FaceEnrollmentStatus.Failed) enrollment.Fail(DateTimeOffset.UtcNow);
        context.AddRange(user, santri, profile, enrollment, NewEmbedding(enrollment.Id, UnitVector(0), 1));
        await context.SaveChangesAsync();
        return santri;
    }

    private static async Task<(Santri Santri, FaceProfile Profile)> AddActiveProfileAsync(AppDbContext context, string label)
    {
        var id = Guid.NewGuid();
        var user = new User(id, $"{label}-{id:N}", label, null, UserRole.Santri, "hash");
        var santri = new Santri(Guid.NewGuid(), user.Id, label, id.ToString("N")[..12], "A", "B", "M", "A", "A");
        var profile = new FaceProfile(Guid.NewGuid(), santri.Id, DateTimeOffset.UtcNow);
        profile.ActivateProfile(DateTimeOffset.UtcNow);
        context.AddRange(user, santri, profile);
        await context.SaveChangesAsync();
        return (santri, profile);
    }

    private static FaceEnrollment NewActiveEnrollment(Guid profileId, string modelName, string modelVersion)
    {
        var enrollment = new FaceEnrollment(Guid.NewGuid(), profileId, modelName, modelVersion, DateTimeOffset.UtcNow);
        enrollment.Activate(DateTimeOffset.UtcNow);
        return enrollment;
    }

    private static FaceEmbedding NewEmbedding(Guid enrollmentId, float[] vector, int captureIndex) =>
        new(Guid.NewGuid(), enrollmentId, vector, captureIndex, .9f);

    private static float[] UnitVector(int index)
    {
        var values = new float[FaceEmbedding.RequiredDimensions];
        values[index] = 1f;
        return values;
    }

    private static float[] Normalized(float first, float second)
    {
        var values = UnitVector(0);
        values[0] = first;
        values[1] = second;
        return values;
    }

    private static async Task AssertInvalidDimensionAsync(NpgsqlConnection connection, Guid enrollmentId, int dimensions)
    {
        await using var command = new NpgsqlCommand("""
            INSERT INTO "FaceEmbeddings" ("Id", "FaceEnrollmentId", "Embedding", "CaptureIndex", "CreatedAtUtc")
            VALUES (@id, @enrollmentId, @embedding, 3, NOW());
            """, connection);
        command.Parameters.AddWithValue("id", Guid.NewGuid());
        command.Parameters.AddWithValue("enrollmentId", enrollmentId);
        command.Parameters.AddWithValue("embedding", ToVectorLiteral(Enumerable.Repeat(.1f, dimensions).ToArray()));

        var error = await Assert.ThrowsAsync<PostgresException>(() => command.ExecuteNonQueryAsync());
        Assert.Equal(PostgresErrorCodes.DatatypeMismatch, error.SqlState);
    }

    private static async Task<string?> ScalarAsync(NpgsqlConnection connection, string sql)
    {
        await using var command = new NpgsqlCommand(sql, connection);
        return (string?)await command.ExecuteScalarAsync();
    }

    private static async Task<double> ScalarDoubleAsync(NpgsqlConnection connection, string sql, string left, string right)
    {
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("left", left);
        command.Parameters.AddWithValue("right", right);
        return (double)(await command.ExecuteScalarAsync())!;
    }

    private static string ToVectorLiteral(IEnumerable<float> values) =>
        $"[{string.Join(',', values.Select(value => value.ToString(System.Globalization.CultureInfo.InvariantCulture)))}]";
}
