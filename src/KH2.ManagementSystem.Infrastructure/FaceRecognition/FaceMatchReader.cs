using KH2.ManagementSystem.Application.Abstractions.FaceRecognition;
using KH2.ManagementSystem.Domain.FaceRecognition;
using KH2.ManagementSystem.Domain.Users;
using KH2.ManagementSystem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Pgvector;

namespace KH2.ManagementSystem.Infrastructure.FaceRecognition;

public sealed class FaceMatchReader(AppDbContext context) : IFaceMatchReader
{
    // Exhaustive search: aggregate before LIMIT so two samples of one person cannot
    // hide a competing identity. No approximate indexes are needed at the initial scale.
    private const string SearchSql = """
        SELECT eligible."SantriId", MAX(1.0 - eligible."Distance") AS "Similarity"
        FROM (
            SELECT profile."SantriId", embedding."Embedding" <=> @probe AS "Distance"
            FROM "FaceEmbeddings" AS embedding
            INNER JOIN "FaceEnrollments" AS enrollment ON enrollment."Id" = embedding."FaceEnrollmentId"
            INNER JOIN "FaceProfiles" AS profile ON profile."Id" = enrollment."FaceProfileId"
            INNER JOIN "Santris" AS santri ON santri."Id" = profile."SantriId"
            INNER JOIN "Users" AS account ON account."Id" = santri."UserId"
            WHERE profile."Status" = @activeStatus
              AND enrollment."Status" = @activeEnrollmentStatus
              AND enrollment."ModelName" = @modelName
              AND enrollment."ModelVersion" = @modelVersion
              AND account."IsActive" = TRUE
              AND account."Role" = @santriRole
        ) AS eligible
        WHERE eligible."Distance" BETWEEN 0.0 AND 2.0
        GROUP BY eligible."SantriId"
        ORDER BY "Similarity" DESC, eligible."SantriId"
        LIMIT 2
        """;

    public async Task<IReadOnlyList<FaceMatchCandidate>> FindNearestAsync(
        FaceEmbeddingResult probe, CancellationToken cancellationToken)
    {
        return await CreateSearchQuery(probe).ToArrayAsync(cancellationToken);
    }

    // Exposed within Infrastructure for SQL translation tests without a live database.
    public IQueryable<FaceMatchCandidate> CreateSearchQuery(FaceEmbeddingResult probe)
    {
        ArgumentNullException.ThrowIfNull(probe);
        if (!probe.Embedding.Any(value => value != 0))
            throw new ArgumentException("A cosine probe must be nonzero.", nameof(probe));
        return context.Database.SqlQueryRaw<FaceMatchCandidate>(SearchSql,
            new NpgsqlParameter("probe", new Vector(probe.Embedding.ToArray())),
            new NpgsqlParameter("activeStatus", nameof(FaceProfileStatus.Active)),
            new NpgsqlParameter("activeEnrollmentStatus", nameof(FaceEnrollmentStatus.Active)),
            new NpgsqlParameter("modelName", probe.ModelName),
            new NpgsqlParameter("modelVersion", probe.ModelVersion),
            new NpgsqlParameter("santriRole", nameof(UserRole.Santri)));
    }
}
