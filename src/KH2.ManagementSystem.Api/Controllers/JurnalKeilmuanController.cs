using System.Security.Claims;
using System.Text.Json;
using System.Globalization;
using KH2.ManagementSystem.Api.Contracts.JurnalKeilmuan;
using KH2.ManagementSystem.Application.Abstractions.Time;
using KH2.ManagementSystem.Domain.JurnalKeilmuans;
using KH2.ManagementSystem.Domain.Santris;
using KH2.ManagementSystem.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KH2.ManagementSystem.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/jurnal-keilmuan")]
public sealed class JurnalKeilmuanController(AppDbContext dbContext, IClock clock) : ControllerBase
{
    [HttpGet("surah")]
    public async Task<ActionResult<IReadOnlyList<QuranSurahOptionResponse>>> GetSurah(CancellationToken cancellationToken)
    {
        if (!await IsKbmMemberAsync(cancellationToken)) return Forbidden();

        var items = await dbContext.QuranSurahs.AsNoTracking()
            .OrderBy(x => x.Number)
            .Select(x => new QuranSurahOptionResponse(x.Id, x.Number, x.Name, x.ArabicName, x.VerseCount))
            .ToArrayAsync(cancellationToken);
        return Ok(items);
    }

    [HttpGet]
    public async Task<ActionResult<JurnalKeilmuanPageResponse>> Get([FromQuery] string? bulan, CancellationToken cancellationToken)
    {
        if (!await IsKbmMemberAsync(cancellationToken)) return Forbidden();
        if (!TryParseMonth(bulan, out var month)) return Invalid("Bulan harus berformat yyyy-MM.");

        var journalRows = await dbContext.JurnalKeilmuans.AsNoTracking()
            .Where(x => x.Tanggal.Year == month.Year && x.Tanggal.Month == month.Month)
            .OrderByDescending(x => x.Tanggal).ThenByDescending(x => x.CreatedAtUtc)
            .ToListAsync(cancellationToken);
        var surahs = await GetSurahMapAsync(cancellationToken);
        var entries = journalRows.Select(x => ToResponse(x, surahs)).ToArray();
        return Ok(new JurnalKeilmuanPageResponse(month.ToString("yyyy-MM", CultureInfo.InvariantCulture), entries, BuildReport(month, entries)));
    }

    [HttpPost]
    public async Task<ActionResult<JurnalKeilmuanItemResponse>> Create([FromBody] SaveJurnalKeilmuanRequest request, CancellationToken cancellationToken)
    {
        var actor = await GetKbmMemberAsync(cancellationToken);
        if (actor is null) return Forbidden();
        var surahs = await GetSurahMapAsync(cancellationToken);
        var validation = ValidateQuran(request, surahs);
        if (validation is not null) return Invalid(validation);

        var journal = new JurnalKeilmuan(Guid.NewGuid(), ToData(request, actor.UserId, actor.FullName));
        await dbContext.JurnalKeilmuans.AddAsync(journal, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        return CreatedAtAction(nameof(Get), new { bulan = journal.Tanggal.ToString("yyyy-MM", CultureInfo.InvariantCulture) }, ToResponse(journal, surahs));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<JurnalKeilmuanItemResponse>> Update(Guid id, [FromBody] SaveJurnalKeilmuanRequest request, CancellationToken cancellationToken)
    {
        var actor = await GetKbmMemberAsync(cancellationToken);
        if (actor is null) return Forbidden();
        var journal = await dbContext.JurnalKeilmuans.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (journal is null) return NotFound();
        var surahs = await GetSurahMapAsync(cancellationToken);
        var validation = ValidateQuran(request, surahs);
        if (validation is not null) return Invalid(validation);

        journal.Update(ToData(request, actor.UserId, actor.FullName), clock.UtcNow);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Ok(ToResponse(journal, surahs));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        if (!await IsKbmMemberAsync(cancellationToken)) return Forbidden();
        var journal = await dbContext.JurnalKeilmuans.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (journal is null) return NotFound();
        dbContext.JurnalKeilmuans.Remove(journal);
        await dbContext.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    private async Task<KbmActor?> GetKbmMemberAsync(CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId)) return null;
        var santri = await dbContext.Santris.AsNoTracking()
            .Where(x => x.UserId == userId)
            .Select(x => new KbmSantriProjection(x.UserId, x.FullName, x.Tim))
            .FirstOrDefaultAsync(cancellationToken);
        return santri is not null && SantriTeam.IsKeilmuan(santri.Tim)
            ? new KbmActor(santri.UserId, santri.FullName)
            : null;
    }

    private async Task<bool> IsKbmMemberAsync(CancellationToken cancellationToken) =>
        await GetKbmMemberAsync(cancellationToken) is not null;

    private async Task<Dictionary<Guid, SurahInfo>> GetSurahMapAsync(CancellationToken cancellationToken) =>
        await dbContext.QuranSurahs.AsNoTracking()
            .OrderBy(x => x.Number)
            .Select(x => new SurahInfo(x.Id, x.Number, x.Name, x.ArabicName, x.VerseCount))
            .ToDictionaryAsync(x => x.Id, cancellationToken);

    private static string? ValidateQuran(SaveJurnalKeilmuanRequest request, Dictionary<Guid, SurahInfo> surahs)
    {
        var hasTarget = request.QuranTargetSurahId.HasValue || request.QuranAyatAwal.HasValue || request.QuranAyatTarget.HasValue;
        if (!hasTarget) return request.QuranRealisasiSurahId.HasValue || request.QuranAyatRealisasi.HasValue
            ? "Realisasi Al-Qur'an memerlukan target Al-Qur'an."
            : null;
        if (!request.QuranTargetSurahId.HasValue || !request.QuranAyatAwal.HasValue || !request.QuranAyatTarget.HasValue ||
            !surahs.TryGetValue(request.QuranTargetSurahId.Value, out var targetSurah))
            return "Pilih surat target dan isi ayat awal serta ayat target.";
        if (request.QuranAyatAwal < 1 || request.QuranAyatTarget < request.QuranAyatAwal || request.QuranAyatTarget > targetSurah.VerseCount)
            return $"Ayat target harus berada pada rentang 1–{targetSurah.VerseCount}.";
        if (request.QuranRealisasiSurahId.HasValue != request.QuranAyatRealisasi.HasValue)
            return "Surat dan ayat realisasi harus diisi bersamaan.";
        if (request.QuranRealisasiSurahId.HasValue &&
            (!surahs.TryGetValue(request.QuranRealisasiSurahId.Value, out var actualSurah) ||
                !request.QuranAyatRealisasi.HasValue ||
                request.QuranAyatRealisasi.Value < 1 ||
                request.QuranAyatRealisasi.Value > actualSurah.VerseCount))
            return "Ayat realisasi tidak sesuai dengan surat yang dipilih.";
        return null;
    }

    private static JurnalKeilmuanData ToData(SaveJurnalKeilmuanRequest request, Guid actorId, string actorName) =>
        new(request.Tanggal, request.SesiSambung, request.Kelas, request.NamaDewanGuru, request.JamMengajar,
            request.AdaMateri, request.JenisMateri, actorId, actorName, request.QuranTargetSurahId,
            request.QuranAyatAwal, request.QuranAyatTarget, request.QuranRealisasiSurahId,
            request.QuranAyatRealisasi, request.QuranKeterangan,
            JsonSerializer.Serialize(request.Details ?? new Dictionary<string, string?>()));

    private static JurnalKeilmuanItemResponse ToResponse(JurnalKeilmuan item, Dictionary<Guid, SurahInfo> surahs)
    {
        surahs.TryGetValue(item.QuranTargetSurahId ?? Guid.Empty, out var target);
        surahs.TryGetValue(item.QuranRealisasiSurahId ?? Guid.Empty, out var actual);
        var metric = CalculateMetric(item, target, actual, surahs.Values);
        var details = JsonSerializer.Deserialize<Dictionary<string, string?>>(item.DetailsJson) ?? [];
        return new JurnalKeilmuanItemResponse(item.Id, item.Tanggal, item.Tanggal.ToString("yyyy-MM", CultureInfo.InvariantCulture), item.SesiSambung,
            item.Kelas, item.NamaDewanGuru, item.JamMengajar, item.AdaMateri, item.JenisMateri, item.DibuatOleh,
            item.QuranTargetSurahId, target?.DisplayName, item.QuranAyatAwal, item.QuranAyatTarget,
            item.QuranRealisasiSurahId, actual?.DisplayName, item.QuranAyatRealisasi, metric.Target, metric.Actual,
            metric.Percentage, metric.OnTarget, item.QuranKeterangan, details, item.CreatedAtUtc, item.UpdatedAtUtc);
    }

    private static Metric CalculateMetric(JurnalKeilmuan item, SurahInfo? target, SurahInfo? actual, IEnumerable<SurahInfo> allSurahs)
    {
        if (target is null || item.QuranAyatAwal is null || item.QuranAyatTarget is null) return new(null, null, null, null);
        var ordered = allSurahs.OrderBy(x => x.Number).ToArray();
        var start = VersePosition(target, item.QuranAyatAwal.Value, ordered);
        var end = VersePosition(target, item.QuranAyatTarget.Value, ordered);
        var targetCount = end - start + 1;
        if (actual is null || item.QuranAyatRealisasi is null) return new(targetCount, null, 0, false);
        var achieved = Math.Clamp(VersePosition(actual, item.QuranAyatRealisasi.Value, ordered) - start + 1, 0, targetCount);
        var percentage = (int)Math.Round((decimal)achieved * 100 / targetCount);
        return new(targetCount, achieved, percentage, achieved >= targetCount);
    }

    private static int VersePosition(SurahInfo surah, int ayat, SurahInfo[] ordered) =>
        ordered.Where(x => x.Number < surah.Number).Sum(x => x.VerseCount) + ayat;

    private static LaporanBulananJurnalResponse BuildReport(DateOnly month, JurnalKeilmuanItemResponse[] entries)
    {
        var targeted = entries.Where(x => x.TargetAyat.HasValue).ToArray();
        var daily = entries.GroupBy(x => x.Tanggal).OrderBy(x => x.Key).Select(group =>
        {
            var dayTargeted = group.Where(x => x.TargetAyat.HasValue).ToArray();
            var completed = dayTargeted.Count(x => x.SesuaiTarget == true);
            var average = dayTargeted.Length == 0 ? 0 : (int)Math.Round(dayTargeted.Average(x => x.PersentaseTarget ?? 0));
            return new LaporanHarianJurnalResponse(group.Key, group.Count(), dayTargeted.Length, completed, average, dayTargeted.Length > 0 && completed == dayTargeted.Length);
        }).ToArray();
        var onTarget = targeted.Count(x => x.SesuaiTarget == true);
        return new LaporanBulananJurnalResponse(month.ToString("yyyy-MM", CultureInfo.InvariantCulture), entries.Length, targeted.Length, onTarget,
            targeted.Length - onTarget, targeted.Length == 0 ? 0 : (int)Math.Round(targeted.Average(x => x.PersentaseTarget ?? 0)), daily);
    }

    private static bool TryParseMonth(string? value, out DateOnly result)
    {
        var parsed = DateOnly.TryParseExact(value ?? DateTime.UtcNow.ToString("yyyy-MM", CultureInfo.InvariantCulture), "yyyy-MM", out result);
        result = parsed ? new DateOnly(result.Year, result.Month, 1) : default;
        return parsed;
    }

    private ObjectResult Forbidden() => Problem(statusCode: StatusCodes.Status403Forbidden, title: "Akses jurnal ditolak", detail: "Fitur Jurnal Keilmuan hanya dapat diakses anggota tim KBM.");
    private ObjectResult Invalid(string detail) => Problem(statusCode: StatusCodes.Status400BadRequest, title: "Data jurnal tidak valid", detail: detail);

    private sealed record KbmActor(Guid UserId, string FullName);
    private sealed record KbmSantriProjection(Guid UserId, string FullName, string Tim);
    private sealed record SurahInfo(Guid Id, int Number, string Name, string ArabicName, int VerseCount)
    { public string DisplayName => $"{Number}. {Name} ({ArabicName})"; }
    private sealed record Metric(int? Target, int? Actual, int? Percentage, bool? OnTarget);
}
