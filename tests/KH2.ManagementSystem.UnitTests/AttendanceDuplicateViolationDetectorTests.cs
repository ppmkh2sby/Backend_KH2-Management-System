using KH2.ManagementSystem.Infrastructure.FaceRecognition;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Xunit;

namespace KH2.ManagementSystem.UnitTests;

public sealed class AttendanceDuplicateViolationDetectorTests
{
    [Fact]
    public void ExpectedSesiSantriUniqueViolationIsRecognized()
    {
        var exception = new DbUpdateException("duplicate", CreatePostgresException(
            PostgresErrorCodes.UniqueViolation,
            AttendanceDuplicateViolationDetector.SesiSantriConstraintName));

        Assert.True(AttendanceDuplicateViolationDetector.IsExpectedSesiSantriDuplicate(exception));
    }

    [Theory]
    [InlineData(PostgresErrorCodes.ForeignKeyViolation, AttendanceDuplicateViolationDetector.SesiSantriConstraintName)]
    [InlineData(PostgresErrorCodes.UniqueViolation, "UX_AnotherConstraint")]
    public void UnrelatedDatabaseFailuresAreNotClassifiedAsAttendanceDuplicates(string sqlState, string constraintName)
    {
        var exception = new DbUpdateException("database failure", CreatePostgresException(sqlState, constraintName));

        Assert.False(AttendanceDuplicateViolationDetector.IsExpectedSesiSantriDuplicate(exception));
    }

    private static PostgresException CreatePostgresException(string sqlState, string constraintName) =>
        new("database failure", "ERROR", "ERROR", sqlState, null, null, 0, 0, null, null,
            "public", "Presensis", null, null, constraintName, null, null, null);
}
