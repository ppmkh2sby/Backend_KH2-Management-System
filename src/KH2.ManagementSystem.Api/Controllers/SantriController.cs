using System.Security.Claims;
using KH2.ManagementSystem.Api.Contracts.Santri;
using KH2.ManagementSystem.Application.Abstractions.Authorization;
using KH2.ManagementSystem.Domain.Santris;
using KH2.ManagementSystem.Domain.Users;
using KH2.ManagementSystem.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KH2.ManagementSystem.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/santri")]
[Route("api/v1/data-santri")]
public sealed class SantriController(
    AppDbContext dbContext,
    ISantriAccessReader santriAccessReader)
    : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(SantriListResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetList(
        [FromQuery] SantriQueryRequest request,
        CancellationToken cancellationToken)
    {
        var context = await GetCurrentUserContextAsync(cancellationToken);

        if (context is null)
        {
            return Unauthorized();
        }

        var page = Math.Max(1, request.Page);
        var perPage = Math.Clamp(request.PerPage, 1, 200);
        var search = request.Search?.Trim().ToLowerInvariant();
        var gender = NormalizeOptionalFilter(request.Gender);
        var tim = NormalizeOptionalFilter(request.Tim);
        var waliSantriIds = await GetAllowedWaliSantriIdsAsync(context, cancellationToken);

        var query = dbContext.Santris.AsNoTracking();

        query = ApplyReadScope(query, context, waliSantriIds);

        if (request.OnlyMine && context.SantriId.HasValue)
        {
            query = query.Where(x => x.Id == context.SantriId.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(x =>
                EF.Functions.ILike(x.FullName, $"%{search}%") ||
                EF.Functions.ILike(x.Nis, $"%{search}%") ||
                EF.Functions.ILike(x.Tim, $"%{search}%") ||
                EF.Functions.ILike(x.Kelas, $"%{search}%") ||
                EF.Functions.ILike(x.Kampus, $"%{search}%"));
        }

        if (!string.IsNullOrWhiteSpace(gender))
        {
            query = query.Where(x => EF.Functions.ILike(x.Gender, gender));
        }

        if (!string.IsNullOrWhiteSpace(tim))
        {
            query = query.Where(x => EF.Functions.ILike(x.Tim, tim));
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderBy(x => x.Gender)
            .ThenBy(x => x.FullName)
            .Skip((page - 1) * perPage)
            .Take(perPage)
            .Select(x => new SantriResponse(
                x.Id,
                x.UserId,
                x.FullName,
                x.Nis,
                x.Kampus,
                x.Jurusan,
                x.Gender,
                x.Tim,
                x.Kelas,
                x.Catatan))
            .ToArrayAsync(cancellationToken);

        return Ok(new SantriListResponse(items, page, perPage, totalCount));
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(SantriResponse), StatusCodes.Status200OK)]
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

        if (!await CanViewSantriAsync(context, id, cancellationToken))
        {
            return Forbid();
        }

        var santri = await dbContext.Santris
            .AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new SantriResponse(
                x.Id,
                x.UserId,
                x.FullName,
                x.Nis,
                x.Kampus,
                x.Jurusan,
                x.Gender,
                x.Tim,
                x.Kelas,
                x.Catatan))
            .FirstOrDefaultAsync(cancellationToken);

        return santri is null ? NotFound() : Ok(santri);
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

    private async Task<List<Guid>> GetAllowedWaliSantriIdsAsync(CurrentUserContext context, CancellationToken cancellationToken)
    {
        if (context.Role != UserRole.WaliSantri)
        {
            return [];
        }

        return await dbContext.WaliSantriRelations
            .AsNoTracking()
            .Where(x => x.WaliUserId == context.UserId)
            .Select(x => x.SantriId)
            .ToListAsync(cancellationToken);
    }

    private async Task<bool> CanViewSantriAsync(CurrentUserContext context, Guid santriId, CancellationToken cancellationToken)
    {
        if (context.IsStaff || context.IsKetertiban)
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

    private static IQueryable<Santri> ApplyReadScope(
        IQueryable<Santri> query,
        CurrentUserContext context,
        List<Guid> waliSantriIds)
    {
        if (context.IsStaff || context.IsKetertiban)
        {
            return query;
        }

        if (context.Role == UserRole.Santri && context.SantriId.HasValue)
        {
            return query.Where(x => x.Id == context.SantriId.Value);
        }

        if (context.Role == UserRole.WaliSantri && waliSantriIds.Count > 0)
        {
            return query.Where(x => waliSantriIds.Contains(x.Id));
        }

        return query.Where(_ => false);
    }

    private static string? NormalizeOptionalFilter(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim().ToLowerInvariant();
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

    private sealed record CurrentUserContext(
        Guid UserId,
        UserRole Role,
        Guid? SantriId,
        bool IsKetertiban)
    {
        public bool IsStaff =>
            Role is UserRole.Admin or UserRole.DewanGuru or UserRole.Pengurus;
    }
}
