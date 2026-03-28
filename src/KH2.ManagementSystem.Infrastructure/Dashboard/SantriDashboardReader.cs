using KH2.ManagementSystem.Application.Abstractions.Dashboard;
using KH2.ManagementSystem.Application.Abstractions.Time;
using KH2.ManagementSystem.Application.Features.Dashboard.GetMySantriAttendance;
using KH2.ManagementSystem.Application.Features.Dashboard.GetMySantriDashboard;
using KH2.ManagementSystem.Application.Features.Dashboard.GetMySantriLog;
using KH2.ManagementSystem.Application.Features.Dashboard.GetMySantriProgress;
using KH2.ManagementSystem.Domain.Kafarahs;
using KH2.ManagementSystem.Domain.LogKeluarMasuks;
using KH2.ManagementSystem.Domain.Users;
using KH2.ManagementSystem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace KH2.ManagementSystem.Infrastructure.Dashboard;

public sealed class SantriDashboardReader(
    AppDbContext dbContext,
    IClock clock)
    : ISantriDashboardReader
{
    public async Task<SantriDashboardDto?> GetOverviewByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var context = await ResolveContextAsync(userId, cancellationToken);

        if (context is null)
        {
            return null;
        }

        var attendanceRows = await LoadAttendanceRowsAsync(context.SantriId, cancellationToken);
        var kafarahRows = await LoadKafarahRowsAsync(context.SantriId, cancellationToken);
        var progressRows = await LoadProgressRowsAsync(context.SantriId, cancellationToken);
        var logRows = await LoadLogRowsAsync(context.SantriId, cancellationToken);

        var attendance = BuildAttendance(attendanceRows);
        var kafarah = BuildKafarah(kafarahRows);
        var progress = BuildProgress(progressRows);
        var logs = BuildLogs(logRows);

        return new SantriDashboardDto(
            clock.UtcNow,
            context.Profile,
            new SantriDashboardHighlightDto(
                attendance.Persentase,
                kafarah.SisaTanggungan,
                progress.Average,
                logs.Total),
            new SantriDashboardOverviewDto(
                attendance.Total,
                attendance.Hadir,
                kafarah.Total,
                kafarah.SisaTanggungan,
                progress.Total,
                progress.Completed,
                logs.Total,
                logs.Tercatat),
            attendance,
            kafarah,
            progress,
            logs);
    }

    public async Task<SantriDashboardAttendancePageDto?> GetAttendanceByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var context = await ResolveContextAsync(userId, cancellationToken);

        if (context is null)
        {
            return null;
        }

        var rows = await LoadAttendanceRowsAsync(context.SantriId, cancellationToken);
        var history = rows.Take(30).ToArray();
        var summary = BuildAttendance(history);
        var issues = history
            .Where(x => IsAttendanceIssue(x.Status))
            .Take(4)
            .ToArray();

        return new SantriDashboardAttendancePageDto(
            clock.UtcNow,
            context.Profile,
            IsAttendanceManager(context),
            new SantriDashboardAttendancePageSummaryDto(
                summary.Total,
                summary.Hadir,
                summary.Izin,
                summary.Alpha,
                summary.Sakit,
                summary.Persentase),
            summary.Recent,
            issues,
            history);
    }

    public async Task<SantriDashboardProgressPageDto?> GetProgressByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var context = await ResolveContextAsync(userId, cancellationToken);

        if (context is null)
        {
            return null;
        }

        var rows = await LoadProgressRowsAsync(context.SantriId, cancellationToken);
        var summary = BuildProgress(rows);

        return new SantriDashboardProgressPageDto(
            clock.UtcNow,
            context.Profile,
            new SantriDashboardProgressPageSummaryDto(
                summary.Total,
                summary.Completed,
                summary.InProgress,
                summary.Average),
            rows.Take(3).ToArray(),
            rows);
    }

    public async Task<SantriDashboardLogPageDto?> GetLogByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var context = await ResolveContextAsync(userId, cancellationToken);

        if (context is null)
        {
            return null;
        }

        var rows = await LoadLogRowsAsync(context.SantriId, cancellationToken);

        return new SantriDashboardLogPageDto(
            clock.UtcNow,
            context.Profile,
            new SantriDashboardLogPageSummaryDto(
                rows.Count,
                rows.Count(x => string.Equals(x.Status, LogKeluarMasuk.StatusApproved, StringComparison.OrdinalIgnoreCase)),
                rows.Count(x => string.Equals(x.Status, LogKeluarMasuk.StatusPending, StringComparison.OrdinalIgnoreCase)),
                rows.Count(x => string.Equals(x.Status, LogKeluarMasuk.StatusRecorded, StringComparison.OrdinalIgnoreCase)),
                rows.Count(x => string.Equals(x.Status, LogKeluarMasuk.StatusRejected, StringComparison.OrdinalIgnoreCase))),
            rows.Take(5).ToArray(),
            rows);
    }

    private static SantriDashboardAttendanceDto BuildAttendance(
        IReadOnlyList<SantriDashboardAttendanceItemDto> rows)
    {
        var hadir = rows.Count(x => string.Equals(x.Status, "hadir", StringComparison.OrdinalIgnoreCase));
        var izin = rows.Count(x => string.Equals(x.Status, "izin", StringComparison.OrdinalIgnoreCase));
        var sakit = rows.Count(x => string.Equals(x.Status, "sakit", StringComparison.OrdinalIgnoreCase));
        var alpa = rows.Count(x => string.Equals(x.Status, "alpa", StringComparison.OrdinalIgnoreCase));
        var total = rows.Count;
        var persentase = total == 0 ? 0 : (int)Math.Round((double)hadir * 100 / total);

        return new SantriDashboardAttendanceDto(
            total,
            hadir,
            izin,
            sakit,
            alpa,
            persentase,
            rows.Take(5).ToArray());
    }

    private static SantriDashboardKafarahDto BuildKafarah(
        SantriDashboardKafarahItemDto[] rows)
    {
        var totalKafarah = rows.Sum(x => x.Tanggungan);
        var jumlahSetor = rows.Sum(x => x.JumlahSetor);

        return new SantriDashboardKafarahDto(
            rows.Length,
            totalKafarah,
            jumlahSetor,
            Math.Max(0, totalKafarah - jumlahSetor),
            rows.Take(5).ToArray());
    }

    private static SantriDashboardProgressDto BuildProgress(
        SantriDashboardProgressItemDto[] rows)
    {
        var completed = rows.Count(x => x.Persentase >= 100);
        var inProgress = rows.Count(x => x.Persentase > 0 && x.Persentase < 100);
        var average = rows.Length == 0 ? 0 : (int)Math.Round(rows.Average(x => x.Persentase));
        var quran = rows.Count(x => ContainsLevel(x.Level, "quran"));
        var hadits = rows.Count(x => ContainsLevel(x.Level, "hadits") || ContainsLevel(x.Level, "hadith"));

        return new SantriDashboardProgressDto(
            rows.Length,
            completed,
            inProgress,
            average,
            quran,
            hadits,
            rows.Take(5).ToArray());
    }

    private static SantriDashboardLogDto BuildLogs(
        List<SantriDashboardLogItemDto> rows)
    {
        var tercatat = rows.Count(x => string.Equals(x.Status, LogKeluarMasuk.StatusRecorded, StringComparison.OrdinalIgnoreCase));

        return new SantriDashboardLogDto(
            rows.Count,
            tercatat,
            rows.Take(5).ToArray());
    }

    private async Task<DashboardContext?> ResolveContextAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        var user = await dbContext.Users
            .AsNoTracking()
            .Where(x => x.Id == userId)
            .Select(x => new
            {
                x.Id,
                x.FullName,
                x.Role,
                x.EmailConfirmed
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (user is null)
        {
            return null;
        }

        var ownSantri = await dbContext.Santris
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .Select(x => new SantriProjection(
                x.Id,
                x.FullName,
                x.Nis,
                x.Kampus,
                x.Jurusan,
                x.Gender,
                x.Tim,
                x.Kelas))
            .FirstOrDefaultAsync(cancellationToken);

        if (ownSantri is not null)
        {
            return new DashboardContext(
                ownSantri.Id,
                ownSantri.Tim,
                BuildProfile(user.FullName, user.Role, user.EmailConfirmed, ownSantri, "self"));
        }

        var fallbackSantri = await dbContext.Santris
            .AsNoTracking()
            .OrderBy(x => x.CreatedAtUtc)
            .ThenBy(x => x.FullName)
            .Select(x => new SantriProjection(
                x.Id,
                x.FullName,
                x.Nis,
                x.Kampus,
                x.Jurusan,
                x.Gender,
                x.Tim,
                x.Kelas))
            .FirstOrDefaultAsync(cancellationToken);

        if (fallbackSantri is not null)
        {
            return new DashboardContext(
                fallbackSantri.Id,
                fallbackSantri.Tim,
                BuildProfile(user.FullName, user.Role, user.EmailConfirmed, fallbackSantri, "fallback"));
        }

        var stub = new SantriProjection(
            Guid.Empty,
            user.FullName,
            string.Empty,
            string.Empty,
            string.Empty,
            string.Empty,
            string.Empty,
            string.Empty);

        return new DashboardContext(
            Guid.Empty,
            string.Empty,
            BuildProfile(user.FullName, user.Role, user.EmailConfirmed, stub, "stub"));
    }

    private static SantriDashboardProfileDto BuildProfile(
        string userFullName,
        UserRole userRole,
        bool emailConfirmed,
        SantriProjection santri,
        string source)
    {
        var fullName = string.IsNullOrWhiteSpace(santri.FullName) ? userFullName : santri.FullName;

        return new SantriDashboardProfileDto(
            santri.Id,
            fullName,
            santri.Nis,
            santri.Kampus,
            santri.Jurusan,
            santri.Gender,
            santri.Tim,
            santri.Kelas,
            emailConfirmed,
            userRole.ToString(),
            source,
            userRole == UserRole.Santri);
    }

    private async Task<List<SantriDashboardAttendanceItemDto>> LoadAttendanceRowsAsync(
        Guid santriId,
        CancellationToken cancellationToken)
    {
        if (santriId == Guid.Empty)
        {
            return [];
        }

        var rows = await (
            from presensi in dbContext.Presensis.AsNoTracking()
            join kegiatan in dbContext.Kegiatans.AsNoTracking() on presensi.KegiatanId equals kegiatan.Id
            join sesi in dbContext.Sesis.AsNoTracking() on presensi.SesiId equals sesi.Id into sesiGroup
            from sesi in sesiGroup.DefaultIfEmpty()
            where presensi.SantriId == santriId
            select new
            {
                presensi.Id,
                Tanggal = sesi != null ? (DateOnly?)sesi.Tanggal : null,
                presensi.Nama,
                KegiatanKategori = kegiatan.Catatan ?? kegiatan.Kategori,
                presensi.Waktu,
                presensi.Status,
                presensi.Catatan,
                presensi.CreatedAtUtc
            })
            .ToListAsync(cancellationToken);

        return rows
            .Select(x => new SantriDashboardAttendanceItemDto(
                x.Id,
                x.Tanggal ?? DateOnly.FromDateTime(x.CreatedAtUtc.UtcDateTime),
                x.Nama,
                x.KegiatanKategori,
                x.Waktu,
                NormalizeAttendanceStatus(x.Status),
                x.Catatan,
                x.CreatedAtUtc))
            .OrderByDescending(x => x.Tanggal)
            .ThenByDescending(x => x.CreatedAtUtc)
            .ToList();
    }

    private async Task<SantriDashboardKafarahItemDto[]> LoadKafarahRowsAsync(
        Guid santriId,
        CancellationToken cancellationToken)
    {
        if (santriId == Guid.Empty)
        {
            return [];
        }

        var rows = await dbContext.Kafarahs
            .AsNoTracking()
            .Where(x => x.SantriId == santriId)
            .OrderByDescending(x => x.Tanggal)
            .ThenByDescending(x => x.CreatedAtUtc)
            .Select(x => new
            {
                x.Id,
                x.Tanggal,
                x.JenisPelanggaran,
                x.KafarahText,
                x.JumlahSetor,
                x.Tanggungan,
                x.Tenggat
            })
            .ToListAsync(cancellationToken);

        return rows
            .Select(x =>
            {
                var definition = Kafarah.ResolveDefinition(x.JenisPelanggaran);

                return new SantriDashboardKafarahItemDto(
                    x.Id,
                    x.Tanggal,
                    x.JenisPelanggaran,
                    definition.Label,
                    x.KafarahText,
                    x.JumlahSetor,
                    x.Tanggungan,
                    Math.Max(0, x.Tanggungan - x.JumlahSetor),
                    x.Tenggat);
            })
            .ToArray();
    }

    private async Task<SantriDashboardProgressItemDto[]> LoadProgressRowsAsync(
        Guid santriId,
        CancellationToken cancellationToken)
    {
        if (santriId == Guid.Empty)
        {
            return [];
        }

        var rows = await dbContext.ProgressKeilmuans
            .AsNoTracking()
            .Where(x => x.SantriId == santriId)
            .OrderByDescending(x => x.UpdatedAtUtc ?? x.CreatedAtUtc)
            .Select(x => new
            {
                x.Id,
                x.Judul,
                x.Target,
                x.Capaian,
                x.Satuan,
                x.Level,
                x.Catatan,
                x.Pembimbing,
                x.TerakhirSetorUtc,
                UpdatedAtUtc = x.UpdatedAtUtc ?? x.CreatedAtUtc
            })
            .ToListAsync(cancellationToken);

        return rows
            .Select(x => new SantriDashboardProgressItemDto(
                x.Id,
                x.Judul,
                x.Target,
                x.Capaian,
                x.Satuan,
                x.Level,
                x.Target <= 0 ? 0 : Math.Min(100, (int)Math.Round((double)x.Capaian * 100 / x.Target)),
                x.Catatan,
                x.Pembimbing,
                x.TerakhirSetorUtc,
                x.UpdatedAtUtc))
            .ToArray();
    }

    private async Task<List<SantriDashboardLogItemDto>> LoadLogRowsAsync(
        Guid santriId,
        CancellationToken cancellationToken)
    {
        if (santriId == Guid.Empty)
        {
            return [];
        }

        return await dbContext.LogKeluarMasuks
            .AsNoTracking()
            .Where(x => x.SantriId == santriId)
            .OrderByDescending(x => x.TanggalPengajuan)
            .ThenByDescending(x => x.CreatedAtUtc)
            .Select(x => new SantriDashboardLogItemDto(
                x.Id,
                x.TanggalPengajuan,
                x.Jenis,
                x.Rentang,
                NormalizeLogStatus(x.Status),
                x.Petugas,
                x.Catatan,
                x.CreatedAtUtc))
            .ToListAsync(cancellationToken);
    }

    private static bool IsAttendanceManager(DashboardContext context)
    {
        return string.Equals(context.Tim, "ketertiban", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsAttendanceIssue(string status)
    {
        return string.Equals(status, "izin", StringComparison.OrdinalIgnoreCase)
            || string.Equals(status, "alpa", StringComparison.OrdinalIgnoreCase);
    }

    private static bool ContainsLevel(string? level, string token)
    {
        return !string.IsNullOrWhiteSpace(level)
            && level.Contains(token, StringComparison.OrdinalIgnoreCase);
    }

    private static string NormalizeAttendanceStatus(string status)
    {
        return status.Trim().ToLowerInvariant() switch
        {
            "alpha" => "alpa",
            "alpa" => "alpa",
            "hadir" => "hadir",
            "izin" => "izin",
            "sakit" => "sakit",
            _ => status.Trim().ToLowerInvariant()
        };
    }

    private static string NormalizeLogStatus(string status)
    {
        return status.Trim().ToLowerInvariant();
    }

    private sealed record DashboardContext(
        Guid SantriId,
        string Tim,
        SantriDashboardProfileDto Profile);

    private sealed record SantriProjection(
        Guid Id,
        string FullName,
        string Nis,
        string Kampus,
        string Jurusan,
        string Gender,
        string Tim,
        string Kelas);
}
