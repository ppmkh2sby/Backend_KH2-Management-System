using System.Globalization;
using System.Security.Claims;
using System.Text;
using KH2.ManagementSystem.Api.Contracts.Presensi;
using KH2.ManagementSystem.Application.Abstractions.Time;
using KH2.ManagementSystem.Domain.Kegiatans;
using KH2.ManagementSystem.Domain.Presensis;
using KH2.ManagementSystem.Domain.Sesis;
using KH2.ManagementSystem.Domain.Users;
using KH2.ManagementSystem.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KH2.ManagementSystem.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/presensi")]
[Route("api/v1/kehadiran")]
public sealed class PresensiController(
    AppDbContext dbContext,
    IClock clock)
    : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(PresensiListResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetList(
        [FromQuery] PresensiQueryRequest request,
        CancellationToken cancellationToken)
    {
        var context = await GetCurrentUserContextAsync(cancellationToken);

        if (context is null)
        {
            return Unauthorized();
        }

        if (!CanAccessAttendanceFeature(context))
        {
            return ForbiddenAttendanceFeature();
        }

        var page = Math.Max(1, request.Page);
        var perPage = Math.Clamp(request.PerPage, 1, 100);
        var normalizedStatus = NormalizeStatus(request.Status);

        if (request.Status is not null && normalizedStatus is null)
        {
            return InvalidRequest("Status presensi tidak valid.");
        }

        var query =
            from presensi in dbContext.Presensis.AsNoTracking()
            join santri in dbContext.Santris.AsNoTracking() on presensi.SantriId equals santri.Id
            join kegiatan in dbContext.Kegiatans.AsNoTracking() on presensi.KegiatanId equals kegiatan.Id
            join sesi in dbContext.Sesis.AsNoTracking() on presensi.SesiId equals sesi.Id into sesiGroup
            from sesi in sesiGroup.DefaultIfEmpty()
            select new
            {
                Presensi = presensi,
                Santri = santri,
                Kegiatan = kegiatan,
                Tanggal = sesi != null ? (DateOnly?)sesi.Tanggal : null
            };

        if (request.SantriId.HasValue)
        {
            query = query.Where(x => x.Presensi.SantriId == request.SantriId.Value);
        }

        if (!string.IsNullOrWhiteSpace(normalizedStatus))
        {
            query = normalizedStatus == "alpa"
                ? query.Where(x => x.Presensi.Status == "alpa" || x.Presensi.Status == "alpha")
                : query.Where(x => x.Presensi.Status == normalizedStatus);
        }

        if (request.TanggalDari.HasValue)
        {
            var fromDate = request.TanggalDari.Value;
            var fromTimestamp = new DateTimeOffset(
                fromDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc),
                TimeSpan.Zero);

            query = query.Where(x =>
                (x.Tanggal.HasValue && x.Tanggal.Value >= fromDate) ||
                (!x.Tanggal.HasValue && x.Presensi.CreatedAtUtc >= fromTimestamp));
        }

        if (request.TanggalSampai.HasValue)
        {
            var toDate = request.TanggalSampai.Value;
            var toTimestamp = new DateTimeOffset(
                toDate.ToDateTime(TimeOnly.MaxValue, DateTimeKind.Utc),
                TimeSpan.Zero);

            query = query.Where(x =>
                (x.Tanggal.HasValue && x.Tanggal.Value <= toDate) ||
                (!x.Tanggal.HasValue && x.Presensi.CreatedAtUtc <= toTimestamp));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var rows = await query
            .OrderByDescending(x => x.Tanggal)
            .ThenByDescending(x => x.Presensi.CreatedAtUtc)
            .Skip((page - 1) * perPage)
            .Take(perPage)
            .Select(x => new PresensiReadRow(
                x.Presensi.Id,
                x.Presensi.SantriId,
                x.Santri.FullName,
                x.Santri.Nis,
                x.Santri.Tim,
                x.Tanggal,
                x.Presensi.Status,
                x.Presensi.Waktu,
                x.Kegiatan.Catatan ?? x.Kegiatan.Kategori,
                x.Presensi.Catatan,
                x.Presensi.CreatedAtUtc,
                x.Presensi.UpdatedAtUtc))
            .ToListAsync(cancellationToken);

        var items = rows
            .Select(x => new PresensiResponse(
                x.Id,
                x.SantriId,
                x.SantriNama,
                x.SantriNis,
                x.SantriTim,
                x.Tanggal ?? DateOnly.FromDateTime(x.CreatedAtUtc.UtcDateTime),
                NormalizeStatus(x.Status) ?? x.Status,
                x.Waktu,
                x.Kegiatan,
                x.Keterangan,
                x.CreatedAtUtc,
                x.UpdatedAtUtc))
            .ToArray();

        return Ok(new PresensiListResponse(items, page, perPage, totalCount));
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(PresensiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var context = await GetCurrentUserContextAsync(cancellationToken);

        if (context is null)
        {
            return Unauthorized();
        }

        if (!CanAccessAttendanceFeature(context))
        {
            return ForbiddenAttendanceFeature();
        }

        var record = await LoadPresensiResponseAsync(id, cancellationToken);

        if (record is null)
        {
            return NotFound();
        }

        return Ok(record);
    }

    [HttpGet("rekap")]
    [HttpGet("/api/v1/kehadiran/rekap")]
    [ProducesResponseType(typeof(PresensiRecapResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetRecap(
        [FromQuery] PresensiRecapQueryRequest request,
        CancellationToken cancellationToken)
    {
        var context = await GetCurrentUserContextAsync(cancellationToken);

        if (context is null)
        {
            return Unauthorized();
        }

        if (!CanAccessAttendanceFeature(context))
        {
            return ForbiddenAttendanceFeature();
        }

        var page = Math.Max(1, request.Page);
        var perPage = Math.Clamp(request.PerPage, 1, 100);
        var month = ParseMonth(request.Bulan) ?? new DateOnly(clock.UtcNow.Year, clock.UtcNow.Month, 1);
        var monthEnd = month.AddMonths(1).AddDays(-1);
        var gender = NormalizeOptionalFilter(request.Gender);
        var kategori = NormalizeOptionalFilter(request.Kategori);
        var waktu = NormalizeWaktu(request.Waktu);
        var santriQuery = dbContext.Santris.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(gender))
        {
            santriQuery = santriQuery.Where(x => EF.Functions.ILike(x.Gender, gender));
        }

        var santris = await santriQuery
            .OrderBy(x => x.FullName)
            .Select(x => new SantriRecapRow(
                x.Id,
                x.FullName,
                x.Nis,
                x.Tim,
                x.Gender))
            .ToListAsync(cancellationToken);

        var santriIds = santris.Select(x => x.Id).ToArray();
        var presensiRows = await (
            from presensi in dbContext.Presensis.AsNoTracking()
            join santri in dbContext.Santris.AsNoTracking() on presensi.SantriId equals santri.Id
            join kegiatan in dbContext.Kegiatans.AsNoTracking() on presensi.KegiatanId equals kegiatan.Id
            join sesi in dbContext.Sesis.AsNoTracking() on presensi.SesiId equals sesi.Id
            where santriIds.Contains(santri.Id)
                && sesi.Tanggal >= month
                && sesi.Tanggal <= monthEnd
            select new
            {
                presensi.SantriId,
                presensi.Status,
                Kategori = kegiatan.Kategori,
                kegiatan.Waktu
            })
            .ToListAsync(cancellationToken);

        if (!string.IsNullOrWhiteSpace(kategori))
        {
            presensiRows = presensiRows
                .Where(x => string.Equals(x.Kategori, kategori, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        if (!string.IsNullOrWhiteSpace(waktu))
        {
            presensiRows = presensiRows
                .Where(x => string.Equals(x.Waktu, waktu, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        var grouped = presensiRows
            .GroupBy(x => x.SantriId)
            .ToDictionary(
                x => x.Key,
                x => new
                {
                    TotalInput = x.Count(),
                    Hadir = x.Count(y => NormalizeStatus(y.Status) == "hadir"),
                    Izin = x.Count(y => NormalizeStatus(y.Status) == "izin"),
                    Sakit = x.Count(y => NormalizeStatus(y.Status) == "sakit"),
                    Alpa = x.Count(y => NormalizeStatus(y.Status) == "alpa")
                });

        var items = santris
            .Select(x =>
            {
                grouped.TryGetValue(x.Id, out var stats);
                var totalInput = stats?.TotalInput ?? 0;
                var hadir = stats?.Hadir ?? 0;
                var izin = stats?.Izin ?? 0;
                var sakit = stats?.Sakit ?? 0;
                var alpa = stats?.Alpa ?? 0;

                return new PresensiRecapItemResponse(
                    x.Id,
                    x.Nama,
                    x.Nis,
                    x.Tim,
                    x.Gender,
                    totalInput,
                    hadir,
                    izin,
                    sakit,
                    alpa,
                    totalInput == 0 ? 0 : (int)Math.Round((double)hadir * 100 / totalInput));
            })
            .OrderByDescending(x => x.Persentase)
            .ThenBy(x => x.Nama)
            .ToArray();

        var summary = new PresensiRecapSummaryResponse(
            santris.Count,
            items.Sum(x => x.TotalInput),
            items.Sum(x => x.TotalInput),
            items.Sum(x => x.Hadir),
            items.Sum(x => x.Izin),
            items.Sum(x => x.Sakit),
            items.Sum(x => x.Alpa),
            items.Sum(x => x.TotalInput) == 0
                ? 0
                : (int)Math.Round((double)items.Sum(x => x.Hadir) * 100 / items.Sum(x => x.TotalInput)));

        var pagedItems = items
            .Skip((page - 1) * perPage)
            .Take(perPage)
            .ToArray();

        return Ok(new PresensiRecapResponse(
            month.ToString("yyyy-MM", CultureInfo.InvariantCulture),
            gender,
            kategori,
            waktu,
            summary,
            pagedItems,
            page,
            perPage,
            items.Length));
    }

    [HttpPost]
    [HttpPost("/api/v1/ketertiban/presensi")]
    [ProducesResponseType(typeof(PresensiResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(PresensiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Store(
        [FromBody] StorePresensiRequest request,
        CancellationToken cancellationToken)
    {
        var context = await GetCurrentUserContextAsync(cancellationToken);

        if (context is null)
        {
            return Unauthorized();
        }

        if (!CanAccessAttendanceFeature(context))
        {
            return ForbiddenAttendanceFeature();
        }

        var normalizedStatus = NormalizeStatus(request.Status);

        if (normalizedStatus is null)
        {
            return InvalidRequest("Status presensi tidak valid.");
        }

        var waktu = NormalizeWaktu(request.Waktu);

        if (waktu is null)
        {
            return InvalidRequest("Waktu presensi tidak valid.");
        }

        var santri = await dbContext.Santris
            .FirstOrDefaultAsync(x => x.Id == request.SantriId, cancellationToken);

        if (santri is null)
        {
            return InvalidRequest("Santri tidak ditemukan.");
        }

        var kegiatan = await FindOrCreateKegiatanAsync(request.Kegiatan, waktu, cancellationToken);
        var sesi = await FindOrCreateSesiAsync(kegiatan.Id, request.Tanggal, cancellationToken);

        var existing = await dbContext.Presensis
            .FirstOrDefaultAsync(
                x => x.SantriId == santri.Id &&
                    x.SesiId == sesi.Id,
                cancellationToken);

        if (existing is not null)
        {
            existing.Update(
                santri.Id,
                santri.FullName,
                normalizedStatus,
                kegiatan.Id,
                sesi.Id,
                request.Keterangan,
                waktu,
                clock.UtcNow);

            await dbContext.SaveChangesAsync(cancellationToken);

            var updated = await LoadPresensiResponseAsync(existing.Id, cancellationToken);
            return Ok(updated);
        }

        var presensi = new Presensi(
            Guid.NewGuid(),
            santri.Id,
            santri.FullName,
            normalizedStatus,
            kegiatan.Id,
            sesi.Id,
            request.Keterangan,
            waktu);

        await dbContext.Presensis.AddAsync(presensi, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        var created = await LoadPresensiResponseAsync(presensi.Id, cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id = presensi.Id }, created);
    }

    [HttpPost("bulk")]
    [HttpPost("/api/v1/ketertiban/presensi/bulk")]
    [ProducesResponseType(typeof(BulkPresensiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> StoreBulk(
        [FromBody] BulkStorePresensiRequest request,
        CancellationToken cancellationToken)
    {
        var context = await GetCurrentUserContextAsync(cancellationToken);

        if (context is null)
        {
            return Unauthorized();
        }

        if (!CanAccessAttendanceFeature(context))
        {
            return ForbiddenAttendanceFeature();
        }

        if (request.Items.Count == 0)
        {
            return InvalidRequest("Daftar presensi wajib diisi.");
        }

        var waktu = NormalizeWaktu(request.Waktu);

        if (waktu is null)
        {
            return InvalidRequest("Waktu presensi tidak valid.");
        }

        var distinctItems = request.Items
            .GroupBy(x => x.SantriId)
            .Select(x => x.Last())
            .ToArray();

        var santriIds = distinctItems.Select(x => x.SantriId).ToArray();
        var santriMap = await dbContext.Santris
            .Where(x => santriIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, cancellationToken);

        if (santriMap.Count != santriIds.Length)
        {
            return InvalidRequest("Sebagian data santri tidak ditemukan.");
        }

        var normalizedItems = new List<(Guid SantriId, string Status)>(distinctItems.Length);
        foreach (var item in distinctItems)
        {
            var normalizedStatus = NormalizeStatus(item.Status);

            if (normalizedStatus is null)
            {
                return InvalidRequest("Status presensi tidak valid.");
            }

            normalizedItems.Add((item.SantriId, normalizedStatus));
        }

        var kegiatan = await FindOrCreateKegiatanAsync(request.Kegiatan, waktu, cancellationToken);
        var sesi = await FindOrCreateSesiAsync(kegiatan.Id, request.Tanggal, cancellationToken);
        var existingRows = await dbContext.Presensis
            .Where(x => x.SesiId == sesi.Id && santriIds.Contains(x.SantriId))
            .ToDictionaryAsync(x => x.SantriId, cancellationToken);

        var createdCount = 0;
        var updatedCount = 0;
        var affectedIds = new List<Guid>(normalizedItems.Count);

        foreach (var item in normalizedItems)
        {
            var santri = santriMap[item.SantriId];

            if (existingRows.TryGetValue(item.SantriId, out var existing))
            {
                existing.Update(
                    santri.Id,
                    santri.FullName,
                    item.Status,
                    kegiatan.Id,
                    sesi.Id,
                    request.Keterangan,
                    waktu,
                    clock.UtcNow);

                updatedCount++;
                affectedIds.Add(existing.Id);
                continue;
            }

            var presensi = new Presensi(
                Guid.NewGuid(),
                santri.Id,
                santri.FullName,
                item.Status,
                kegiatan.Id,
                sesi.Id,
                request.Keterangan,
                waktu);

            await dbContext.Presensis.AddAsync(presensi, cancellationToken);
            createdCount++;
            affectedIds.Add(presensi.Id);
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        var rows = new List<PresensiResponse>(affectedIds.Count);
        foreach (var affectedId in affectedIds)
        {
            var row = await LoadPresensiResponseAsync(affectedId, cancellationToken);
            if (row is not null)
            {
                rows.Add(row);
            }
        }

        return Ok(new BulkPresensiResponse(rows, createdCount, updatedCount));
    }

    [HttpPatch("{id:guid}")]
    [HttpPatch("/api/v1/ketertiban/presensi/{id:guid}")]
    [ProducesResponseType(typeof(PresensiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdatePresensiRequest request,
        CancellationToken cancellationToken)
    {
        var context = await GetCurrentUserContextAsync(cancellationToken);

        if (context is null)
        {
            return Unauthorized();
        }

        if (!CanAccessAttendanceFeature(context))
        {
            return ForbiddenAttendanceFeature();
        }

        var presensi = await dbContext.Presensis
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (presensi is null)
        {
            return NotFound();
        }

        var santri = await dbContext.Santris
            .FirstOrDefaultAsync(x => x.Id == (request.SantriId ?? presensi.SantriId), cancellationToken);

        if (santri is null)
        {
            return InvalidRequest("Santri tidak ditemukan.");
        }

        var kegiatanData = await (
            from kegiatanRow in dbContext.Kegiatans.AsNoTracking()
            join sesiRow in dbContext.Sesis.AsNoTracking() on kegiatanRow.Id equals sesiRow.KegiatanId into sesiGroup
            from sesiRecord in sesiGroup.Where(x => x.Id == presensi.SesiId).DefaultIfEmpty()
            where kegiatanRow.Id == presensi.KegiatanId
            select new
            {
                kegiatanRow.Kategori,
                kegiatanRow.Catatan,
                Tanggal = sesiRecord != null ? (DateOnly?)sesiRecord.Tanggal : null
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (kegiatanData is null)
        {
            return InvalidRequest("Data kegiatan presensi tidak ditemukan.");
        }

        var normalizedStatus = request.Status is null
            ? presensi.Status
            : NormalizeStatus(request.Status);

        if (normalizedStatus is null)
        {
            return InvalidRequest("Status presensi tidak valid.");
        }

        var waktu = request.Waktu is null
            ? presensi.Waktu
            : NormalizeWaktu(request.Waktu);

        if (waktu is null)
        {
            return InvalidRequest("Waktu presensi tidak valid.");
        }

        var tanggal = request.Tanggal
            ?? kegiatanData.Tanggal
            ?? DateOnly.FromDateTime(presensi.CreatedAtUtc.UtcDateTime);

        var kegiatan = request.Kegiatan is null && waktu == presensi.Waktu
            ? await dbContext.Kegiatans.FirstAsync(x => x.Id == presensi.KegiatanId, cancellationToken)
            : await FindOrCreateKegiatanAsync(
                request.Kegiatan ?? kegiatanData.Catatan ?? kegiatanData.Kategori,
                waktu,
                cancellationToken);

        var sesi = (tanggal == kegiatanData.Tanggal && kegiatan.Id == presensi.KegiatanId && presensi.SesiId.HasValue)
            ? await dbContext.Sesis.FirstAsync(x => x.Id == presensi.SesiId.Value, cancellationToken)
            : await FindOrCreateSesiAsync(kegiatan.Id, tanggal, cancellationToken);

        presensi.Update(
            santri.Id,
            santri.FullName,
            normalizedStatus,
            kegiatan.Id,
            sesi.Id,
            request.Keterangan ?? presensi.Catatan,
            waktu,
            clock.UtcNow);

        await dbContext.SaveChangesAsync(cancellationToken);

        var updated = await LoadPresensiResponseAsync(presensi.Id, cancellationToken);
        return Ok(updated);
    }

    [HttpDelete("{id:guid}")]
    [HttpDelete("/api/v1/ketertiban/presensi/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var context = await GetCurrentUserContextAsync(cancellationToken);

        if (context is null)
        {
            return Unauthorized();
        }

        if (!CanAccessAttendanceFeature(context))
        {
            return ForbiddenAttendanceFeature();
        }

        var presensi = await dbContext.Presensis
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (presensi is null)
        {
            return NotFound();
        }

        dbContext.Presensis.Remove(presensi);
        await dbContext.SaveChangesAsync(cancellationToken);

        return NoContent();
    }

    private async Task<CurrentUserContext?> GetCurrentUserContextAsync(CancellationToken cancellationToken)
    {
        var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var roleValue = User.FindFirstValue(ClaimTypes.Role);

        if (!Guid.TryParse(userIdValue, out var userId))
        {
            return null;
        }

        if (!Enum.TryParse<UserRole>(roleValue, ignoreCase: true, out var role))
        {
            return null;
        }

        var santri = await dbContext.Santris
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .Select(x => new
            {
                x.Id,
                x.Tim
            })
            .FirstOrDefaultAsync(cancellationToken);

        var isKetertiban = IsKetertibanTeam(santri?.Tim, role);

        return new CurrentUserContext(
            userId,
            role,
            santri?.Id,
            isKetertiban);
    }

    private static bool CanAccessAttendanceFeature(CurrentUserContext context)
    {
        return context.Role == UserRole.Santri && context.IsKetertiban;
    }

    private async Task<Kegiatan> FindOrCreateKegiatanAsync(
        string? kegiatanInput,
        string waktu,
        CancellationToken cancellationToken)
    {
        var label = string.IsNullOrWhiteSpace(kegiatanInput)
            ? "Presensi harian"
            : kegiatanInput.Trim();

        var category = Slugify(label, 30);

        var kegiatan = await dbContext.Kegiatans
            .FirstOrDefaultAsync(x => x.Kategori == category && x.Waktu == waktu, cancellationToken);

        if (kegiatan is not null)
        {
            return kegiatan;
        }

        kegiatan = new Kegiatan(
            Guid.NewGuid(),
            category,
            waktu,
            label);

        await dbContext.Kegiatans.AddAsync(kegiatan, cancellationToken);
        return kegiatan;
    }

    private async Task<Sesi> FindOrCreateSesiAsync(
        Guid kegiatanId,
        DateOnly tanggal,
        CancellationToken cancellationToken)
    {
        var sesi = await dbContext.Sesis
            .FirstOrDefaultAsync(
                x => x.KegiatanId == kegiatanId &&
                    x.Tanggal == tanggal,
                cancellationToken);

        if (sesi is not null)
        {
            return sesi;
        }

        sesi = new Sesi(Guid.NewGuid(), kegiatanId, tanggal);
        await dbContext.Sesis.AddAsync(sesi, cancellationToken);

        return sesi;
    }

    private async Task<PresensiResponse?> LoadPresensiResponseAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        var row = await (
            from presensi in dbContext.Presensis.AsNoTracking()
            join santri in dbContext.Santris.AsNoTracking() on presensi.SantriId equals santri.Id
            join kegiatan in dbContext.Kegiatans.AsNoTracking() on presensi.KegiatanId equals kegiatan.Id
            join sesi in dbContext.Sesis.AsNoTracking() on presensi.SesiId equals sesi.Id into sesiGroup
            from sesi in sesiGroup.DefaultIfEmpty()
            where presensi.Id == id
            select new
            {
                presensi.Id,
                presensi.SantriId,
                SantriNama = santri.FullName,
                SantriNis = santri.Nis,
                SantriTim = santri.Tim,
                Tanggal = sesi != null ? (DateOnly?)sesi.Tanggal : null,
                presensi.Status,
                presensi.Waktu,
                Kegiatan = kegiatan.Catatan ?? kegiatan.Kategori,
                Keterangan = presensi.Catatan,
                presensi.CreatedAtUtc,
                presensi.UpdatedAtUtc
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (row is null)
        {
            return null;
        }

        return new PresensiResponse(
            row.Id,
            row.SantriId,
            row.SantriNama,
            row.SantriNis,
            row.SantriTim,
            row.Tanggal ?? DateOnly.FromDateTime(row.CreatedAtUtc.UtcDateTime),
            NormalizeStatus(row.Status) ?? row.Status,
            row.Waktu,
            row.Kegiatan,
            row.Keterangan,
            row.CreatedAtUtc,
            row.UpdatedAtUtc);
    }

    private static string? NormalizeStatus(string? status)
    {
        if (string.IsNullOrWhiteSpace(status))
        {
            return null;
        }

        return status.Trim().ToLowerInvariant() switch
        {
            "hadir" => "hadir",
            "izin" => "izin",
            "alpa" => "alpa",
            "alpha" => "alpa",
            "sakit" => "sakit",
            _ => null
        };
    }

    private static string? NormalizeWaktu(string? waktu)
    {
        var normalized = string.IsNullOrWhiteSpace(waktu)
            ? "pagi"
            : waktu.Trim().ToLowerInvariant();

        return Presensi.Times.Contains(normalized, StringComparer.OrdinalIgnoreCase)
            ? normalized
            : null;
    }

    private static string? NormalizeOptionalFilter(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim().ToLowerInvariant();
    }

    private static DateOnly? ParseMonth(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return DateOnly.TryParseExact(
            $"{value.Trim()}-01",
            "yyyy-MM-dd",
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out var parsed)
            ? parsed
            : null;
    }

    private static bool IsKetertibanTeam(string? tim, UserRole role)
    {
        if (role != UserRole.Santri)
        {
            return false;
        }

        return string.Equals(tim, "KTB", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(tim, "ketertiban", StringComparison.OrdinalIgnoreCase);
    }

    private static string Slugify(string input, int maxLength)
    {
        var builder = new StringBuilder(input.Length);
        var previousDash = false;

        foreach (var ch in input.ToLowerInvariant())
        {
            if (char.IsLetterOrDigit(ch))
            {
                builder.Append(ch);
                previousDash = false;
            }
            else if (!previousDash)
            {
                builder.Append('-');
                previousDash = true;
            }

            if (builder.Length >= maxLength)
            {
                break;
            }
        }

        var value = builder.ToString().Trim('-');
        return string.IsNullOrWhiteSpace(value) ? "presensi" : value;
    }

    private BadRequestObjectResult InvalidRequest(string detail)
    {
        return BadRequest(new ProblemDetails
        {
            Title = "Permintaan presensi tidak valid.",
            Detail = detail,
            Status = StatusCodes.Status400BadRequest
        });
    }

    private ObjectResult ForbiddenAttendanceFeature()
    {
        return StatusCode(StatusCodes.Status403Forbidden, new ProblemDetails
        {
            Title = "Akses presensi ditolak.",
            Detail = "Fitur kehadiran santri hanya dapat digunakan oleh santri yang termasuk tim KTB.",
            Status = StatusCodes.Status403Forbidden
        });
    }

    private sealed record CurrentUserContext(
        Guid UserId,
        UserRole Role,
        Guid? SantriId,
        bool IsKetertiban)
    {
        public bool IsStaff =>
            Role is UserRole.Admin or UserRole.DewanGuru or UserRole.Pengurus;
    }

    private sealed record PresensiReadRow(
        Guid Id,
        Guid SantriId,
        string SantriNama,
        string SantriNis,
        string SantriTim,
        DateOnly? Tanggal,
        string Status,
        string Waktu,
        string Kegiatan,
        string? Keterangan,
        DateTimeOffset CreatedAtUtc,
        DateTimeOffset? UpdatedAtUtc);

    private sealed record SantriRecapRow(
        Guid Id,
        string Nama,
        string Nis,
        string Tim,
        string Gender);
}
