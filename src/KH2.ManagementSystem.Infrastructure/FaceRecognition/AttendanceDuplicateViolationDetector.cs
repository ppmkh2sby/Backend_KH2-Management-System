using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace KH2.ManagementSystem.Infrastructure.FaceRecognition;

public static class AttendanceDuplicateViolationDetector
{
    public const string SesiSantriConstraintName = "UX_Presensis_SesiId_SantriId";

    public static bool IsExpectedSesiSantriDuplicate(DbUpdateException exception) =>
        exception.InnerException is PostgresException
        {
            SqlState: PostgresErrorCodes.UniqueViolation,
            ConstraintName: SesiSantriConstraintName
        };
}
