using System.Globalization;
using System.Security.Claims;
using KH2.ManagementSystem.Api.Contracts.Kafarah;
using KH2.ManagementSystem.Application.Abstractions.Time;
using KH2.ManagementSystem.Domain.Kafarahs;
using KH2.ManagementSystem.Domain.Users;
using KH2.ManagementSystem.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KH2.ManagementSystem.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/kafarah")]
[Route("api/v1/ketertiban/kafarah")]
public sealed class KafarahController(
    AppDbContext dbContext,
    IClock clock)
    : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(KafarahListResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetList(
        [FromQuery] KafarahQueryRequest request,
        CancellationToken cancellationToken)
    {
        var context = await GetCurrentUserContextAsync(cancellationToken);

        if (context is null)
        {
            return Unauthorized();
        }

        if (!CanAccessKafarahFeature(context))
        {
            return ForbiddenKafarahFeature();
        }

        var page = Math.Max(1, request.Page);
        var perPage = Math.Clamp(request.PerPage, 1, 100);
        var search = request.Search?.Trim().ToLowerInvariant();
        var gender = NormalizeOptional(request.Gender);
        var tim = NormalizeOptional(request.Tim);

        var query =
            from kafarah in dbContext.Kafarahs.AsNoTracking()
            join santri in dbContext.Santris.AsNoTracking() on kafarah.SantriId equals santri.Id
            select new
            {
                Kafarah = kafarah,
                Santri = santri
            };

        if (request.SantriId.HasValue)
        {
            query = query.Where(x => x.Kafarah.SantriId == request.SantriId.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(x =>
                EF.Functions.ILike(x.Santri.FullName, $"%{search}%") ||
                EF.Functions.ILike(x.Santri.Tim, $"%{search}%") ||
                EF.Functions.ILike(x.Kafarah.JenisPelanggaran, $"%{search}%") ||
                EF.Functions.ILike(x.Kafarah.KafarahText, $"%{search}%"));
        }

        if (!string.IsNullOrWhiteSpace(gender))
        {
            query = query.Where(x => EF.Functions.ILike(x.Santri.Gender, gender));
        }

        if (!string.IsNullOrWhiteSpace(tim))
        {
            query = query.Where(x => EF.Functions.ILike(x.Santri.Tim, tim));
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var rows = await query
            .OrderByDescending(x => x.Kafarah.Tanggal)
            .ThenBy(x => x.Santri.FullName)
            .Skip((page - 1) * perPage)
            .Take(perPage)
            .Select(x => new KafarahReadRow(
                x.Kafarah.Id,
                x.Kafarah.SantriId,
                x.Santri.FullName,
                x.Santri.Nis,
                x.Santri.Tim,
                x.Santri.Gender,
                x.Kafarah.Tanggal,
                x.Kafarah.JenisPelanggaran,
                x.Kafarah.KafarahText,
                x.Kafarah.JumlahSetor,
                x.Kafarah.Tanggungan,
                x.Kafarah.Tenggat,
                x.Kafarah.CreatedAtUtc,
                x.Kafarah.UpdatedAtUtc))
            .ToListAsync(cancellationToken);

        return Ok(new KafarahListResponse(
            rows.Select(MapResponse).ToArray(),
            page,
            perPage,
            totalCount));
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(KafarahResponse), StatusCodes.Status200OK)]
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

        if (!CanAccessKafarahFeature(context))
        {
            return ForbiddenKafarahFeature();
        }

        var row = await LoadKafarahRowAsync(id, cancellationToken);
        if (row is null)
        {
            return NotFound();
        }

        return Ok(MapResponse(row));
    }

    [HttpPost]
    [ProducesResponseType(typeof(KafarahResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Store(
        [FromBody] StoreKafarahRequest request,
        CancellationToken cancellationToken)
    {
        var context = await GetCurrentUserContextAsync(cancellationToken);

        if (context is null)
        {
            return Unauthorized();
        }

        if (!CanAccessKafarahFeature(context))
        {
            return ForbiddenKafarahFeature();
        }

        var santri = await dbContext.Santris
            .FirstOrDefaultAsync(x => x.Id == request.SantriId, cancellationToken);

        if (santri is null)
        {
            return InvalidRequest("Santri tidak ditemukan.");
        }

        var definition = ResolveDefinition(request.JenisPelanggaran);
        var jumlahSetor = 0;
        var tanggungan = definition.DefaultAmount;
        var tenggat = NormalizeTenggat(null, request.Tanggal);

        var kafarah = new Kafarah(
            Guid.NewGuid(),
            santri.Id,
            request.Tanggal,
            definition.Code,
            definition.Description,
            jumlahSetor,
            tanggungan,
            tenggat);

        await dbContext.Kafarahs.AddAsync(kafarah, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        var created = await LoadKafarahRowAsync(kafarah.Id, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = kafarah.Id }, MapResponse(created!));
    }

    [HttpPost("bulk")]
    [ProducesResponseType(typeof(BulkKafarahResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> StoreBulk(
        [FromBody] BulkStoreKafarahRequest request,
        CancellationToken cancellationToken)
    {
        var context = await GetCurrentUserContextAsync(cancellationToken);

        if (context is null)
        {
            return Unauthorized();
        }

        if (!CanAccessKafarahFeature(context))
        {
            return ForbiddenKafarahFeature();
        }

        if (request.SantriIds.Count == 0)
        {
            return InvalidRequest("Daftar santri wajib diisi.");
        }

        var definition = ResolveDefinition(request.JenisPelanggaran);
        var jumlahSetor = 0;
        var tanggungan = definition.DefaultAmount;
        var tenggat = NormalizeTenggat(null, request.Tanggal);
        var santriIds = request.SantriIds.Distinct().ToArray();

        var existingSantriIds = await dbContext.Santris
            .AsNoTracking()
            .Where(x => santriIds.Contains(x.Id))
            .Select(x => x.Id)
            .ToListAsync(cancellationToken);

        if (existingSantriIds.Count != santriIds.Length)
        {
            return InvalidRequest("Sebagian data santri tidak ditemukan.");
        }

        var createdIds = new List<Guid>(santriIds.Length);
        foreach (var santriId in santriIds)
        {
            var kafarah = new Kafarah(
                Guid.NewGuid(),
                santriId,
                request.Tanggal,
                definition.Code,
                definition.Description,
                jumlahSetor,
                tanggungan,
                tenggat);

            await dbContext.Kafarahs.AddAsync(kafarah, cancellationToken);
            createdIds.Add(kafarah.Id);
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        var rows = await (
            from kafarah in dbContext.Kafarahs.AsNoTracking()
            join santri in dbContext.Santris.AsNoTracking() on kafarah.SantriId equals santri.Id
            where createdIds.Contains(kafarah.Id)
            orderby kafarah.Tanggal descending, santri.FullName
            select new KafarahReadRow(
                kafarah.Id,
                kafarah.SantriId,
                santri.FullName,
                santri.Nis,
                santri.Tim,
                santri.Gender,
                kafarah.Tanggal,
                kafarah.JenisPelanggaran,
                kafarah.KafarahText,
                kafarah.JumlahSetor,
                kafarah.Tanggungan,
                kafarah.Tenggat,
                kafarah.CreatedAtUtc,
                kafarah.UpdatedAtUtc))
            .ToListAsync(cancellationToken);

        return Ok(new BulkKafarahResponse(rows.Select(MapResponse).ToArray(), rows.Count, 0));
    }

    [HttpPatch("{id:guid}")]
    [ProducesResponseType(typeof(KafarahResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdateKafarahRequest request,
        CancellationToken cancellationToken)
    {
        var context = await GetCurrentUserContextAsync(cancellationToken);

        if (context is null)
        {
            return Unauthorized();
        }

        if (!CanAccessKafarahFeature(context))
        {
            return ForbiddenKafarahFeature();
        }

        var kafarah = await dbContext.Kafarahs
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (kafarah is null)
        {
            return NotFound();
        }

        var jenis = request.JenisPelanggaran is null
            ? kafarah.JenisPelanggaran
            : ResolveDefinition(request.JenisPelanggaran).Code;

        var definition = Kafarah.ResolveDefinition(jenis);
        var tanggal = request.Tanggal ?? kafarah.Tanggal;
        var jumlahSetor = Math.Max(0, request.JumlahSetor ?? kafarah.JumlahSetor);
        var tanggunganBaseline = request.JenisPelanggaran is null
            ? kafarah.Tanggungan
            : definition.DefaultAmount;
        var tanggungan = Math.Max(jumlahSetor, request.Tanggungan ?? tanggunganBaseline);
        var tenggat = request.Tenggat is null
            ? kafarah.Tenggat
            : NormalizeTenggat(request.Tenggat, tanggal);

        kafarah.Update(
            tanggal,
            definition.Code,
            definition.Description,
            jumlahSetor,
            tanggungan,
            tenggat,
            clock.UtcNow);

        await dbContext.SaveChangesAsync(cancellationToken);

        var updated = await LoadKafarahRowAsync(id, cancellationToken);
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

        if (!CanAccessKafarahFeature(context))
        {
            return ForbiddenKafarahFeature();
        }

        var kafarah = await dbContext.Kafarahs
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (kafarah is null)
        {
            return NotFound();
        }

        dbContext.Kafarahs.Remove(kafarah);
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
            .Select(x => new { x.Id, x.Tim })
            .FirstOrDefaultAsync(cancellationToken);

        return new CurrentUserContext(
            userId,
            role,
            santri?.Id,
            IsKetertibanTeam(santri?.Tim, role));
    }

    private async Task<KafarahReadRow?> LoadKafarahRowAsync(Guid id, CancellationToken cancellationToken)
    {
        return await (
            from kafarah in dbContext.Kafarahs.AsNoTracking()
            join santri in dbContext.Santris.AsNoTracking() on kafarah.SantriId equals santri.Id
            where kafarah.Id == id
            select new KafarahReadRow(
                kafarah.Id,
                kafarah.SantriId,
                santri.FullName,
                santri.Nis,
                santri.Tim,
                santri.Gender,
                kafarah.Tanggal,
                kafarah.JenisPelanggaran,
                kafarah.KafarahText,
                kafarah.JumlahSetor,
                kafarah.Tanggungan,
                kafarah.Tenggat,
                kafarah.CreatedAtUtc,
                kafarah.UpdatedAtUtc))
            .FirstOrDefaultAsync(cancellationToken);
    }

    private static KafarahDefinition ResolveDefinition(string value)
    {
        var normalized = NormalizeRequired(value);
        return Kafarah.ResolveDefinition(normalized);
    }

    private static string NormalizeRequired(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Value is required.", nameof(value));
        }

        return value.Trim().ToLowerInvariant();
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim().ToLowerInvariant();
    }

    private static string NormalizeTenggat(string? value, DateOnly tanggal)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            return value.Trim();
        }

        return tanggal.AddDays(7).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
    }

    private static bool CanAccessKafarahFeature(CurrentUserContext context) =>
        context.Role == UserRole.Santri && context.IsKetertiban;

    private static bool IsKetertibanTeam(string? tim, UserRole role)
    {
        if (role != UserRole.Santri)
        {
            return false;
        }

        return string.Equals(tim, "KTB", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(tim, "ketertiban", StringComparison.OrdinalIgnoreCase);
    }

    private static KafarahResponse MapResponse(KafarahReadRow row)
    {
        var definition = Kafarah.ResolveDefinition(row.JenisPelanggaran);

        return new KafarahResponse(
            row.Id,
            row.SantriId,
            row.SantriNama,
            row.SantriNis,
            row.SantriTim,
            row.SantriGender,
            row.Tanggal,
            row.JenisPelanggaran,
            definition.Label,
            row.Kafarah,
            row.JumlahSetor,
            row.Tanggungan,
            Math.Max(0, row.Tanggungan - row.JumlahSetor),
            row.Tenggat,
            row.CreatedAtUtc,
            row.UpdatedAtUtc);
    }

    private BadRequestObjectResult InvalidRequest(string detail)
    {
        return BadRequest(new ProblemDetails
        {
            Title = "Permintaan kafarah tidak valid.",
            Detail = detail,
            Status = StatusCodes.Status400BadRequest
        });
    }

    private ObjectResult ForbiddenKafarahFeature()
    {
        return StatusCode(StatusCodes.Status403Forbidden, new ProblemDetails
        {
            Title = "Akses kafarah ditolak.",
            Detail = "Fitur kafarah santri hanya dapat digunakan oleh santri yang termasuk tim KTB.",
            Status = StatusCodes.Status403Forbidden
        });
    }

    private sealed record CurrentUserContext(
        Guid UserId,
        UserRole Role,
        Guid? SantriId,
        bool IsKetertiban)
    ;

    private sealed record KafarahReadRow(
        Guid Id,
        Guid SantriId,
        string SantriNama,
        string SantriNis,
        string SantriTim,
        string SantriGender,
        DateOnly Tanggal,
        string JenisPelanggaran,
        string Kafarah,
        int JumlahSetor,
        int Tanggungan,
        string? Tenggat,
        DateTimeOffset CreatedAtUtc,
        DateTimeOffset? UpdatedAtUtc);
}
