using KH2.ManagementSystem.Domain.FaceRecognition;
using KH2.ManagementSystem.Infrastructure.FaceRecognition;
using KH2.ManagementSystem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Xunit;

namespace KH2.ManagementSystem.UnitTests;

[Collection(PostgreSqlIntegrationFixtureDefinition.Name)]
public sealed class PostgreSqlAuditPersistenceTests(PostgreSqlIntegrationFixture fixture)
{
    [Fact]
    public async Task AttendanceDevicePersistsRotationAndRejectsDuplicateNamesWithoutMisclassifyingIt()
    {
        await using var context = await fixture.CreateContextAsync();
        var now = DateTimeOffset.UtcNow;
        var device = new AttendanceDevice(Guid.NewGuid(), "Gate A", "test-hash-one", now, "Gate");
        device.RotateKey("test-hash-two", now.AddMinutes(1));
        context.AttendanceDevices.Add(device);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var stored = await context.AttendanceDevices.SingleAsync();
        Assert.Equal("test-hash-two", stored.ApiKeyHash);
        Assert.Equal(2, stored.KeyVersion);
        Assert.Equal(now.AddMinutes(1).ToUnixTimeMilliseconds(), stored.KeyRotatedAtUtc!.Value.ToUnixTimeMilliseconds());

        context.AttendanceDevices.Add(new AttendanceDevice(Guid.NewGuid(), "Gate A", "other-hash", now));
        var violation = await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
        Assert.Equal(PostgresErrorCodes.UniqueViolation, ((PostgresException)violation.InnerException!).SqlState);
        Assert.False(AttendanceDuplicateViolationDetector.IsExpectedSesiSantriDuplicate(violation));

        await using var connection = new NpgsqlConnection(fixture.ConnectionString);
        await connection.OpenAsync();
        Assert.Equal(0L, await CountAsync(connection, """
            SELECT COUNT(*) FROM information_schema.columns
            WHERE table_name = 'AttendanceDevices'
              AND (column_name ILIKE '%plaintext%' OR (column_name ILIKE '%apikey%' AND column_name <> 'ApiKeyHash'));
            """));
    }

    [Fact]
    public async Task FaceRecognitionEventsRoundTripCanonicalStringsAndAllowInvalidSessionWithoutAuditFks()
    {
        await using var context = await fixture.CreateContextAsync();
        var outcomes = new[]
        {
            (RecognitionOutcome.Recognized, AttendanceOutcome.Recorded),
            (RecognitionOutcome.Recognized, AttendanceOutcome.Duplicate),
            (RecognitionOutcome.NotAttempted, AttendanceOutcome.InvalidSession),
            (RecognitionOutcome.Unknown, AttendanceOutcome.NotAttempted),
            (RecognitionOutcome.Rejected, AttendanceOutcome.NotAttempted),
            (RecognitionOutcome.Error, AttendanceOutcome.Failed)
        };
        foreach (var (recognition, attendance) in outcomes)
            context.FaceRecognitionEvents.Add(new FaceRecognitionEvent(Guid.NewGuid(), null, null, null, null, null, null,
                FaceRecognitionEventSource.AttendanceDevice, recognition, attendance, null, null, "SafeCode", 1, DateTimeOffset.UtcNow));
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
        var stored = await context.FaceRecognitionEvents.OrderBy(item => item.CreatedAtUtc).ToListAsync();
        Assert.Equal(6, stored.Count);
        Assert.Contains(stored, item => item.RecognitionOutcome == RecognitionOutcome.NotAttempted && item.AttendanceOutcome == AttendanceOutcome.InvalidSession && item.SantriId is null && item.PresensiId is null);

        await using var connection = new NpgsqlConnection(fixture.ConnectionString);
        await connection.OpenAsync();
        Assert.Equal(0L, await CountAsync(connection, """
            SELECT COUNT(*) FROM information_schema.columns
            WHERE table_name = 'FaceRecognitionEvents'
              AND (column_name ILIKE '%image%' OR column_name ILIKE '%embedding%' OR column_name ILIKE '%apikey%' OR column_name ILIKE '%jwt%' OR column_name ILIKE '%password%');
            """));
    }

    [Fact]
    public async Task DeletingAnAttendanceDeviceRetainsItsRecognitionEventAndSetsTheAuditForeignKeyToNull()
    {
        await using var context = await fixture.CreateContextAsync();
        var device = new AttendanceDevice(Guid.NewGuid(), "SetNull gate", "test-hash", DateTimeOffset.UtcNow);
        var auditEvent = new FaceRecognitionEvent(Guid.NewGuid(), device.Id, null, null, null, null, null,
            FaceRecognitionEventSource.AttendanceDevice, RecognitionOutcome.Unknown, AttendanceOutcome.NotAttempted,
            null, null, null, 1, DateTimeOffset.UtcNow);
        context.AddRange(device, auditEvent);
        await context.SaveChangesAsync();

        context.AttendanceDevices.Remove(device);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var retained = await context.FaceRecognitionEvents.SingleAsync(item => item.Id == auditEvent.Id);
        Assert.Null(retained.DeviceId);
    }

    private static async Task<long> CountAsync(NpgsqlConnection connection, string sql)
    {
        await using var command = new NpgsqlCommand(sql, connection);
        return (long)(await command.ExecuteScalarAsync())!;
    }
}
