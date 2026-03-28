using System.Globalization;
using System.Security.Claims;
using KH2.ManagementSystem.Api.Contracts.LogKeluarMasuk;
using KH2.ManagementSystem.Application.Abstractions.Time;
using KH2.ManagementSystem.Domain.LogKeluarMasuks;
using KH2.ManagementSystem.Domain.Users;
using KH2.ManagementSystem.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KH2.ManagementSystem.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/log-keluar-masuk")]
public sealed class LogKeluarMasukController(
    AppDbContext dbContext,
    IClock clock)
    : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(LogKeluarMasukListResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetList(
        [FromQuery] LogKeluarMasukQueryRequest request,
        CancellationToken cancellationToken)
    {
        var context = await GetCurrentUserContextAsync(cancellationToken);
        if (context is null)
        {
            return Unauthorized();
        }

        if (!CanAccessLogFeature(context))
        {
            return ForbiddenLogFeature();
        }

        var page = Math.Max(1, request.Page);
        var perPage = Math.Clamp(request.PerPage, 1, 100);
        var search = request.Search?.Trim().ToLowerInvariant();
        var gender = NormalizeOptional(request.Gender);
        var isTeamScope = context.CanViewAll && string.Equals(request.Scope, "team", StringComparison.OrdinalIgnoreCase);

        var query =
            from log in dbContext.LogKeluarMasuks.AsNoTracking()
            join santri in dbContext.Santris.AsNoTracking() on log.SantriId equals santri.Id
            select new
            {
                Log = log,
                Santri = santri
            };

        if (!isTeamScope)
        {
            if (!context.SantriId.HasValue)
            {
                return ForbiddenLogFeature();
            }

            query = query.Where(x => x.Log.SantriId == context.SantriId.Value);
        }
        else
        {
            if (!string.IsNullOrWhiteSpace(gender))
            {
                query = query.Where(x => EF.Functions.ILike(x.Santri.Gender, gender));
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(x =>
                    EF.Functions.ILike(x.Log.Jenis, $"%{search}%") ||
                    EF.Functions.ILike(x.Log.Catatan ?? string.Empty, $"%{search}%") ||
                    EF.Functions.ILike(x.Santri.FullName, $"%{search}%"));
            }
        }

        if (!isTeamScope && !string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(x =>
                EF.Functions.ILike(x.Log.Jenis, $"%{search}%") ||
                EF.Functions.ILike(x.Log.Catatan ?? string.Empty, $"%{search}%"));
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var rows = await query
            .OrderByDescending(x => x.Log.TanggalPengajuan)
            .ThenByDescending(x => x.Log.CreatedAtUtc)
            .Skip((page - 1) * perPage)
            .Take(perPage)
            .Select(x => new LogReadRow(
                x.Log.Id,
                x.Log.SantriId,
                x.Santri.FullName,
                x.Santri.Nis,
                x.Santri.Tim,
                x.Santri.Gender,
                x.Log.TanggalPengajuan,
                x.Log.Jenis,
                x.Log.Rentang,
                x.Log.Status,
                x.Log.Catatan,
                x.Log.CreatedAtUtc,
                x.Log.UpdatedAtUtc))
            .ToListAsync(cancellationToken);

        return Ok(new LogKeluarMasukListResponse(
            rows.Select(MapResponse).ToArray(),
            page,
            perPage,
            totalCount));
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(LogKeluarMasukResponse), StatusCodes.Status200OK)]
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

        if (!CanAccessLogFeature(context))
        {
            return ForbiddenLogFeature();
        }

        var row = await LoadLogRowAsync(id, cancellationToken);
        if (row is null)
        {
            return NotFound();
        }

        if (!context.CanViewAll && row.SantriId != context.SantriId)
        {
            return ForbiddenLogFeature();
        }

        return Ok(MapResponse(row));
    }

    [HttpPost]
    [ProducesResponseType(typeof(LogKeluarMasukResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Store(
        [FromBody] StoreLogKeluarMasukRequest request,
        CancellationToken cancellationToken)
    {
        var context = await GetCurrentUserContextAsync(cancellationToken);
        if (context is null)
        {
            return Unauthorized();
        }

        if (context.Role != UserRole.Santri || !context.SantriId.HasValue)
        {
            return ForbiddenManageLogFeature();
        }

        if (!TryBuildRentang(request.WaktuKeluar, request.WaktuMasuk, out var rentang))
        {
            return InvalidRequest("Format waktu keluar/masuk tidak valid.");
        }

        var log = new LogKeluarMasuk(
            Guid.NewGuid(),
            context.SantriId.Value,
            request.Tanggal,
            request.Tujuan,
            rentang,
            LogKeluarMasuk.StatusRecorded,
            null,
            request.Catatan);

        await dbContext.LogKeluarMasuks.AddAsync(log, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        var created = await LoadLogRowAsync(log.Id, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = log.Id }, MapResponse(created!));
    }

    [HttpPatch("{id:guid}")]
    [ProducesResponseType(typeof(LogKeluarMasukResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdateLogKeluarMasukRequest request,
        CancellationToken cancellationToken)
    {
        var context = await GetCurrentUserContextAsync(cancellationToken);
        if (context is null)
        {
            return Unauthorized();
        }

        if (context.Role != UserRole.Santri || !context.SantriId.HasValue)
        {
            return ForbiddenManageLogFeature();
        }

        if (!TryBuildRentang(request.WaktuKeluar, request.WaktuMasuk, out var rentang))
        {
            return InvalidRequest("Format waktu keluar/masuk tidak valid.");
        }

        var log = await dbContext.LogKeluarMasuks
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (log is null)
        {
            return NotFound();
        }

        if (log.SantriId != context.SantriId.Value)
        {
            return ForbiddenManageLogFeature();
        }

        log.Update(
            request.Tanggal,
            request.Tujuan,
            rentang,
            LogKeluarMasuk.StatusRecorded,
            log.Petugas,
            request.Catatan,
            clock.UtcNow);

        await dbContext.SaveChangesAsync(cancellationToken);

        var updated = await LoadLogRowAsync(log.Id, cancellationToken);
        return Ok(MapResponse(updated!));
    }

    [HttpDelete("{id:guid}")]
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

        if (context.Role != UserRole.Santri || !context.SantriId.HasValue)
        {
            return ForbiddenManageLogFeature();
        }

        var log = await dbContext.LogKeluarMasuks
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (log is null)
        {
            return NotFound();
        }

        if (log.SantriId != context.SantriId.Value)
        {
            return ForbiddenManageLogFeature();
        }

        dbContext.LogKeluarMasuks.Remove(log);
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

        if (!Enum.TryParse<UserRole>(roleValue, true, out var role))
        {
            return null;
        }

        var santriId = await dbContext.Santris
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .Select(x => (Guid?)x.Id)
            .FirstOrDefaultAsync(cancellationToken);

        var canViewAll = role is UserRole.Admin or UserRole.DewanGuru or UserRole.Pengurus;
        return new CurrentUserContext(userId, role, santriId, canViewAll);
    }

    private async Task<LogReadRow?> LoadLogRowAsync(Guid id, CancellationToken cancellationToken)
    {
        return await (
            from log in dbContext.LogKeluarMasuks.AsNoTracking()
            join santri in dbContext.Santris.AsNoTracking() on log.SantriId equals santri.Id
            where log.Id == id
            select new LogReadRow(
                log.Id,
                log.SantriId,
                santri.FullName,
                santri.Nis,
                santri.Tim,
                santri.Gender,
                log.TanggalPengajuan,
                log.Jenis,
                log.Rentang,
                log.Status,
                log.Catatan,
                log.CreatedAtUtc,
                log.UpdatedAtUtc))
            .FirstOrDefaultAsync(cancellationToken);
    }

    private static LogKeluarMasukResponse MapResponse(LogReadRow row)
    {
        var (waktuKeluar, waktuMasuk) = SplitRentang(row.Rentang);
        return new LogKeluarMasukResponse(
            row.Id,
            row.SantriId,
            row.SantriNama,
            row.SantriNis,
            row.SantriTim,
            row.SantriGender,
            row.Tanggal,
            row.Tujuan,
            waktuKeluar,
            waktuMasuk,
            row.Rentang,
            row.Status,
            row.Catatan,
            row.CreatedAtUtc,
            row.UpdatedAtUtc);
    }

    private static (string? WaktuKeluar, string? WaktuMasuk) SplitRentang(string? rentang)
    {
        if (string.IsNullOrWhiteSpace(rentang))
        {
            return (null, null);
        }

        var parts = rentang.Split('-', 2, StringSplitOptions.TrimEntries);
        return (parts.ElementAtOrDefault(0), parts.ElementAtOrDefault(1));
    }

    private static bool TryBuildRentang(string waktuKeluar, string waktuMasuk, out string rentang)
    {
        rentang = string.Empty;
        if (!TimeOnly.TryParseExact(waktuKeluar, "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out var keluar))
        {
            return false;
        }

        if (!TimeOnly.TryParseExact(waktuMasuk, "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out var masuk))
        {
            return false;
        }

        rentang = $"{keluar:HH\\:mm} - {masuk:HH\\:mm}";
        return true;
    }

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static bool CanAccessLogFeature(CurrentUserContext context) =>
        context.Role is UserRole.Santri or UserRole.Admin or UserRole.DewanGuru or UserRole.Pengurus;

    private ObjectResult ForbiddenLogFeature() =>
        Problem(
            statusCode: StatusCodes.Status403Forbidden,
            title: "Forbidden",
            detail: "Fitur log keluar/masuk hanya dapat digunakan oleh santri, pengurus, atau dewan guru.");

    private ObjectResult ForbiddenManageLogFeature() =>
        Problem(
            statusCode: StatusCodes.Status403Forbidden,
            title: "Forbidden",
            detail: "Hanya santri yang dapat mengelola log keluar/masuk miliknya.");

    private ObjectResult InvalidRequest(string detail) =>
        Problem(
            statusCode: StatusCodes.Status400BadRequest,
            title: "Invalid Request",
            detail: detail);

    private sealed record CurrentUserContext(Guid UserId, UserRole Role, Guid? SantriId, bool CanViewAll);
    private sealed record LogReadRow(
        Guid Id,
        Guid SantriId,
        string SantriNama,
        string SantriNis,
        string SantriTim,
        string SantriGender,
        DateOnly Tanggal,
        string Tujuan,
        string? Rentang,
        string Status,
        string? Catatan,
        DateTimeOffset CreatedAtUtc,
        DateTimeOffset? UpdatedAtUtc);
}
