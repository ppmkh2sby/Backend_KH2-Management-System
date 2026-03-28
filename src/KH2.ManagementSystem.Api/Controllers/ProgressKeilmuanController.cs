using System.Security.Claims;
using KH2.ManagementSystem.Api.Contracts.ProgressKeilmuan;
using KH2.ManagementSystem.Application.Abstractions.Authorization;
using KH2.ManagementSystem.Application.Abstractions.Time;
using KH2.ManagementSystem.Domain.ProgressKeilmuans;
using KH2.ManagementSystem.Domain.Users;
using KH2.ManagementSystem.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KH2.ManagementSystem.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/progress-keilmuan")]
[Route("api/v1/progres-keilmuan")]
public sealed class ProgressKeilmuanController(
    AppDbContext dbContext,
    IClock clock,
    ISantriAccessReader santriAccessReader)
    : ControllerBase
{
    private static readonly ProgressModuleDefinition[] QuranModules =
        Enumerable.Range(1, 30)
            .Select(index => new ProgressModuleDefinition($"Juz {index}", 20))
            .ToArray();

    private static readonly ProgressModuleDefinition[] HaditsModules =
    [
        new("Mukhtaru Da'awat", 171),
        new("Buku Saku Tata Krama", 60),
        new("Al Khulasoh Fii Adabith Tholib", 20),
        new("Materi Kelas Bacaan", 37),
        new("K. Sholah", 151),
        new("Al-Khulashoh Fil Imla'", 20),
        new("Luzumul Jama'ah", 40),
        new("Materi Kelas Pegon", 30),
        new("K. Sholatin Nawafil", 98),
        new("K. Shoum", 98),
        new("Materi Kelas Lambatan", 47),
        new("K. Da'awat", 65),
        new("K. Adab", 95),
        new("K. Shifatil Jannati Wannar", 85),
        new("K. Janaiz", 79),
        new("K. Adillah", 96),
        new("K. Manasik wal Jihad", 51),
        new("Materi Kelas Cepatan", 70),
        new("K. Haji", 111),
        new("K. Manaskil Haji", 116),
        new("K. Ahkam", 124),
        new("K. Jihad", 63),
        new("K. Imaroh", 104),
        new("K. Imaroh min Kanzil ummal", 122),
        new("Khutbah", 152),
        new("Materi Kelas Saringan", 63),
        new("Hidayatul Mustafidz Fit-Tajwid", 98),
        new("K. Nikah", 101),
        new("K. Faroidh", 134),
        new("Syarah Asmaullohul Husna", 39),
        new("Syarah Do'a ASAD", 35),
    ];

    [HttpGet]
    [ProducesResponseType(typeof(ProgressKeilmuanPageResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProgressKeilmuanStaffPageResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProgressKeilmuanDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(
        [FromQuery] ProgressKeilmuanQueryRequest request,
        CancellationToken cancellationToken)
    {
        var context = await GetCurrentUserContextAsync(cancellationToken);
        if (context is null)
        {
            return Unauthorized();
        }

        var category = ResolveCategory(request.Category);

        if (context.IsStaff)
        {
            var response = await BuildStaffPageResponseAsync(request, category, cancellationToken);
            return Ok(response);
        }

        if (context.Role == UserRole.WaliSantri)
        {
            var firstChildCode = await dbContext.WaliSantriRelations
                .AsNoTracking()
                .Where(x => x.WaliUserId == context.UserId)
                .Join(
                    dbContext.Santris.AsNoTracking(),
                    relation => relation.SantriId,
                    santri => santri.Id,
                    (_, santri) => new { santri.FullName, santri.Nis })
                .OrderBy(x => x.FullName)
                .Select(x => x.Nis)
                .FirstOrDefaultAsync(cancellationToken);

            if (string.IsNullOrWhiteSpace(firstChildCode))
            {
                return MissingWaliRelation();
            }

            var detail = await BuildDetailResponseAsync(context, firstChildCode, cancellationToken);
            return detail is null ? NotFound() : Ok(detail);
        }

        if (context.Role != UserRole.Santri || !context.SantriId.HasValue)
        {
            return ForbiddenProgressFeature();
        }

        var mine = await BuildPageResponseAsync(
            context.SantriId.Value,
            category,
            recentLimit: 5,
            weightedAverage: false,
            cancellationToken);

        return Ok(mine);
    }

    [HttpGet("staff")]
    [ProducesResponseType(typeof(ProgressKeilmuanStaffPageResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetStaff(
        [FromQuery] ProgressKeilmuanQueryRequest request,
        CancellationToken cancellationToken)
    {
        var context = await GetCurrentUserContextAsync(cancellationToken);
        if (context is null)
        {
            return Unauthorized();
        }

        if (!context.IsStaff)
        {
            return ForbiddenProgressFeature();
        }

        var category = ResolveCategory(request.Category);
        var response = await BuildStaffPageResponseAsync(request, category, cancellationToken);
        return Ok(response);
    }

    [HttpGet("{santriCode}/detail")]
    [ProducesResponseType(typeof(ProgressKeilmuanDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetDetail(
        string santriCode,
        CancellationToken cancellationToken)
    {
        var context = await GetCurrentUserContextAsync(cancellationToken);
        if (context is null)
        {
            return Unauthorized();
        }

        var detail = await BuildDetailResponseAsync(context, santriCode, cancellationToken);
        if (detail is not null)
        {
            return Ok(detail);
        }

        var exists = await dbContext.Santris
            .AsNoTracking()
            .AnyAsync(x => x.Nis == santriCode.Trim(), cancellationToken);

        return exists ? Forbid() : NotFound();
    }

    [HttpPost("sync")]
    [ProducesResponseType(typeof(ProgressKeilmuanPageResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Sync(
        [FromBody] SyncProgressKeilmuanRequest request,
        CancellationToken cancellationToken)
    {
        var context = await GetCurrentUserContextAsync(cancellationToken);
        if (context is null)
        {
            return Unauthorized();
        }

        if (context.Role != UserRole.Santri || !context.SantriId.HasValue)
        {
            return ForbiddenProgressFeature();
        }

        if (request.Modules.Count == 0)
        {
            return InvalidRequest("Daftar modul progress wajib diisi.");
        }

        var category = ResolveCategory(request.Category);
        var moduleMap = GetModules(category).ToDictionary(x => x.Judul, StringComparer.OrdinalIgnoreCase);
        var payloadMap = request.Modules
            .Where(x => !string.IsNullOrWhiteSpace(x.Judul))
            .GroupBy(x => x.Judul.Trim(), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(x => x.Key, x => x.Last(), StringComparer.OrdinalIgnoreCase);

        foreach (var payload in payloadMap.Values)
        {
            if (!moduleMap.TryGetValue(payload.Judul.Trim(), out var module))
            {
                continue;
            }

            if (payload.Value.HasValue && payload.Value.Value > module.Target)
            {
                return InvalidRequest($"Progres {module.Judul} maksimal {module.Target} halaman.");
            }
        }

        var existingRows = await dbContext.ProgressKeilmuans
            .Where(x => x.SantriId == context.SantriId.Value && x.Level == category)
            .ToListAsync(cancellationToken);

        var existingMap = existingRows.ToDictionary(x => x.Judul, StringComparer.OrdinalIgnoreCase);
        foreach (var module in GetModules(category))
        {
            payloadMap.TryGetValue(module.Judul, out var payload);
            var value = payload?.Value;
            existingMap.TryGetValue(module.Judul, out var existing);

            if (!value.HasValue)
            {
                if (existing is not null)
                {
                    dbContext.ProgressKeilmuans.Remove(existing);
                }

                continue;
            }

            if (existing is not null)
            {
                existing.Update(
                    module.Judul,
                    module.Target,
                    value.Value,
                    "halaman",
                    category,
                    existing.Catatan,
                    existing.Pembimbing,
                    clock.UtcNow,
                    clock.UtcNow);
            }
            else
            {
                await dbContext.ProgressKeilmuans.AddAsync(
                    new ProgressKeilmuan(
                        Guid.NewGuid(),
                        context.SantriId.Value,
                        module.Judul,
                        module.Target,
                        value.Value,
                        "halaman",
                        category,
                        null,
                        null,
                        clock.UtcNow),
                    cancellationToken);
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        var response = await BuildPageResponseAsync(
            context.SantriId.Value,
            category,
            recentLimit: 5,
            weightedAverage: false,
            cancellationToken);

        return Ok(response);
    }

    private async Task<CurrentUserContext?> GetCurrentUserContextAsync(CancellationToken cancellationToken)
    {
        var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var roleValue = User.FindFirstValue(ClaimTypes.Role);

        if (!Guid.TryParse(userIdValue, out var userId))
        {
            return null;
        }

        if (!Enum.TryParse<UserRole>(roleValue, true, out var role))
        {
            return null;
        }

        var santriId = await dbContext.Santris
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .Select(x => (Guid?)x.Id)
            .FirstOrDefaultAsync(cancellationToken);

        return new CurrentUserContext(userId, role, santriId);
    }

    private async Task<ProgressKeilmuanPageResponse> BuildPageResponseAsync(
        Guid santriId,
        string category,
        int recentLimit,
        bool weightedAverage,
        CancellationToken cancellationToken)
    {
        var section = await BuildSectionResponseAsync(
            santriId,
            category,
            recentLimit,
            weightedAverage,
            cancellationToken);

        return new ProgressKeilmuanPageResponse(
            section.Category,
            section.Summary,
            section.Modules,
            section.RecentUpdates);
    }

    private async Task<ProgressKeilmuanDetailResponse?> BuildDetailResponseAsync(
        CurrentUserContext context,
        string santriCode,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(santriCode))
        {
            return null;
        }

        var normalizedCode = santriCode.Trim();
        var santri = await dbContext.Santris
            .AsNoTracking()
            .Where(x => x.Nis == normalizedCode)
            .Select(x => new DetailSantriProjection(
                x.Id,
                x.FullName,
                x.Nis,
                x.Gender,
                x.Tim,
                x.Kelas))
            .FirstOrDefaultAsync(cancellationToken);

        if (santri is null)
        {
            return null;
        }

        if (!await CanAccessSantriAsync(context, santri.Id, cancellationToken))
        {
            return null;
        }

        var quran = await BuildSectionResponseAsync(
            santri.Id,
            ProgressKeilmuan.LevelQuran,
            recentLimit: 8,
            weightedAverage: true,
            cancellationToken);

        var hadits = await BuildSectionResponseAsync(
            santri.Id,
            ProgressKeilmuan.LevelHadits,
            recentLimit: 8,
            weightedAverage: true,
            cancellationToken);

        return new ProgressKeilmuanDetailResponse(
            new ProgressKeilmuanDetailSantriResponse(
                santri.Id,
                santri.Nama,
                santri.Nis,
                NormalizeGenderValue(santri.Gender),
                santri.Tim,
                santri.Kelas),
            quran,
            hadits);
    }

    private async Task<ProgressKeilmuanDetailSectionResponse> BuildSectionResponseAsync(
        Guid santriId,
        string category,
        int recentLimit,
        bool weightedAverage,
        CancellationToken cancellationToken)
    {
        var modules = GetModules(category);
        var rows = await dbContext.ProgressKeilmuans
            .AsNoTracking()
            .Where(x => x.SantriId == santriId && x.Level == category)
            .ToListAsync(cancellationToken);

        var rowMap = rows.ToDictionary(x => x.Judul, StringComparer.OrdinalIgnoreCase);
        var items = modules
            .Select(module =>
            {
                rowMap.TryGetValue(module.Judul, out var row);
                var value = row?.Capaian;
                var persentase = value.HasValue
                    ? CalculatePercentage(value.Value, module.Target)
                    : 0;

                return new ProgressKeilmuanModuleResponse(
                    module.Judul,
                    module.Target,
                    value,
                    persentase,
                    row?.TerakhirSetorUtc ?? row?.UpdatedAtUtc);
            })
            .ToArray();

        var total = items.Length;
        var completed = items.Count(x => x.Persentase >= 100);
        var inProgress = items.Count(x => x.Persentase > 0 && x.Persentase < 100);
        var average = weightedAverage
            ? CalculateWeightedAverage(items)
            : total == 0
                ? 0
                : (int)Math.Round(items.Average(x => x.Persentase));

        var recent = rows
            .OrderByDescending(x => x.TerakhirSetorUtc ?? x.UpdatedAtUtc ?? x.CreatedAtUtc)
            .Take(recentLimit)
            .Select(x => new ProgressKeilmuanRecentResponse(
                x.Id,
                x.Judul,
                x.Capaian,
                x.Target,
                x.Satuan,
                x.Persentase,
                x.TerakhirSetorUtc,
                x.UpdatedAtUtc))
            .ToArray();

        return new ProgressKeilmuanDetailSectionResponse(
            category,
            new ProgressKeilmuanSummaryResponse(total, completed, inProgress, average),
            items,
            recent);
    }

    private async Task<ProgressKeilmuanStaffPageResponse> BuildStaffPageResponseAsync(
        ProgressKeilmuanQueryRequest request,
        string category,
        CancellationToken cancellationToken)
    {
        var page = Math.Max(1, request.Page);
        var perPage = Math.Clamp(request.PerPage, 1, 100);
        var search = request.Search?.Trim().ToLowerInvariant();
        var gender = NormalizeGenderFilter(request.Gender);

        var santriQuery = dbContext.Santris.AsNoTracking();

        if (gender != "all")
        {
            santriQuery = santriQuery.Where(x => EF.Functions.ILike(x.Gender, gender));
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            santriQuery = santriQuery.Where(x =>
                EF.Functions.ILike(x.FullName, $"%{search}%") ||
                EF.Functions.ILike(x.Nis, $"%{search}%") ||
                EF.Functions.ILike(x.Tim, $"%{search}%") ||
                EF.Functions.ILike(x.Kelas, $"%{search}%"));
        }

        var santriRows = await santriQuery
            .Select(x => new StaffSantriProjection(
                x.Id,
                x.Nis,
                x.FullName,
                x.Gender,
                x.Kelas,
                x.Tim))
            .ToListAsync(cancellationToken);

        var santriIds = santriRows.Select(x => x.Id).ToArray();
        var progressRows = santriIds.Length == 0
            ? []
            : await dbContext.ProgressKeilmuans
                .AsNoTracking()
                .Where(x => x.Level == category && santriIds.Contains(x.SantriId))
                .Select(x => new StaffProgressProjection(
                    x.SantriId,
                    x.Capaian,
                    x.Target,
                    x.TerakhirSetorUtc,
                    x.UpdatedAtUtc,
                    x.CreatedAtUtc))
                .ToListAsync(cancellationToken);

        var aggregateMap = progressRows
            .GroupBy(x => x.SantriId)
            .ToDictionary(
                group => group.Key,
                group =>
                {
                    var percentages = group
                        .Select(x => CalculatePercentage(x.Capaian, x.Target))
                        .ToArray();

                    return new StaffProgressAggregate(
                        percentages.Count(x => x >= 100),
                        percentages.Count(x => x > 0 && x < 100),
                        percentages.Length == 0 ? 0 : (int)Math.Round(percentages.Average()),
                        group.Max(x => x.TerakhirSetorUtc ?? x.UpdatedAtUtc ?? x.CreatedAtUtc));
                });

        var rows = santriRows
            .Select(santri =>
            {
                aggregateMap.TryGetValue(santri.Id, out var aggregate);

                return new ProgressKeilmuanStaffRowResponse(
                    santri.Id,
                    santri.Code,
                    santri.Nama,
                    NormalizeGenderValue(santri.Gender),
                    santri.Kelas,
                    santri.Tim,
                    aggregate?.Completed ?? 0,
                    aggregate?.InProgress ?? 0,
                    aggregate?.Average ?? 0,
                    aggregate?.UpdatedAtUtc);
            })
            .OrderByDescending(x => x.Average)
            .ThenBy(x => x.Nama, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var totalCount = rows.Length;
        var pagedRows = rows
            .Skip((page - 1) * perPage)
            .Take(perPage)
            .ToArray();

        return new ProgressKeilmuanStaffPageResponse(
            category,
            new ProgressKeilmuanStaffSummaryResponse(
                totalCount,
                rows.Count(x => x.Completed + x.InProgress > 0),
                totalCount == 0 ? 0 : (int)Math.Round(rows.Average(x => x.Average)),
                rows.Sum(x => x.Completed),
                GetModules(category).Length),
            pagedRows,
            page,
            perPage,
            totalCount);
    }

    private async Task<bool> CanAccessSantriAsync(
        CurrentUserContext context,
        Guid santriId,
        CancellationToken cancellationToken)
    {
        if (context.IsStaff)
        {
            return true;
        }

        if (context.Role == UserRole.Santri)
        {
            return await santriAccessReader.IsSantriOwnerAsync(context.UserId, santriId, cancellationToken);
        }

        if (context.Role == UserRole.WaliSantri)
        {
            return await santriAccessReader.IsWaliOfSantriAsync(context.UserId, santriId, cancellationToken);
        }

        return false;
    }

    private static string ResolveCategory(string? category) =>
        string.Equals(category?.Trim(), ProgressKeilmuan.LevelHadits, StringComparison.OrdinalIgnoreCase)
            ? ProgressKeilmuan.LevelHadits
            : ProgressKeilmuan.LevelQuran;

    private static ProgressModuleDefinition[] GetModules(string category) =>
        string.Equals(category, ProgressKeilmuan.LevelHadits, StringComparison.OrdinalIgnoreCase)
            ? HaditsModules
            : QuranModules;

    private static int CalculatePercentage(int capaian, int target)
    {
        if (target <= 0)
        {
            return 0;
        }

        return Math.Min(100, (int)Math.Round((double)capaian * 100 / target));
    }

    private static int CalculateWeightedAverage(ProgressKeilmuanModuleResponse[] items)
    {
        var targetTotal = items.Sum(x => Math.Max(x.Target, 0));
        var capaianTotal = items.Sum(x =>
        {
            var value = Math.Max(x.Value ?? 0, 0);
            return x.Target > 0 ? Math.Min(value, x.Target) : value;
        });

        if (targetTotal > 0)
        {
            return (int)Math.Round((double)capaianTotal * 100 / targetTotal);
        }

        return items.Length == 0 ? 0 : (int)Math.Round(items.Average(x => x.Persentase));
    }

    private static string NormalizeGenderFilter(string? value)
    {
        var normalized = value?.Trim().ToLowerInvariant();
        return normalized is "putra" or "putri" ? normalized : "all";
    }

    private static string NormalizeGenderValue(string value)
    {
        var normalized = value.Trim().ToLowerInvariant();

        if (normalized is "putra" or "l" or "lk" or "laki-laki" or "laki" or "male" or "ikhwan")
        {
            return "putra";
        }

        if (normalized is "putri" or "p" or "pr" or "perempuan" or "female" or "akhwat")
        {
            return "putri";
        }

        return value.Trim();
    }

    private ObjectResult ForbiddenProgressFeature() =>
        Problem(
            statusCode: StatusCodes.Status403Forbidden,
            title: "Akses progres ditolak",
            detail: "Anda tidak memiliki akses ke fitur progres keilmuan ini.");

    private ObjectResult MissingWaliRelation() =>
        Problem(
            statusCode: StatusCodes.Status404NotFound,
            title: "Data anak tidak ditemukan",
            detail: "Akun wali belum terhubung ke data anak.");

    private ObjectResult InvalidRequest(string detail) =>
        Problem(
            statusCode: StatusCodes.Status400BadRequest,
            title: "Invalid Request",
            detail: detail);

    private sealed record CurrentUserContext(Guid UserId, UserRole Role, Guid? SantriId)
    {
        public bool IsStaff => Role is UserRole.Admin or UserRole.DewanGuru or UserRole.Pengurus;
    }

    private sealed record ProgressModuleDefinition(string Judul, int Target);
    private sealed record DetailSantriProjection(Guid Id, string Nama, string Nis, string Gender, string Tim, string Kelas);
    private sealed record StaffSantriProjection(Guid Id, string Code, string Nama, string Gender, string Kelas, string Tim);
    private sealed record StaffProgressProjection(
        Guid SantriId,
        int Capaian,
        int Target,
        DateTimeOffset? TerakhirSetorUtc,
        DateTimeOffset? UpdatedAtUtc,
        DateTimeOffset CreatedAtUtc);
    private sealed record StaffProgressAggregate(
        int Completed,
        int InProgress,
        int Average,
        DateTimeOffset? UpdatedAtUtc);
}
