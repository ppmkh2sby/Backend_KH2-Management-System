namespace KH2.ManagementSystem.Application.Features.Dashboard.GetMySantriDashboard;

public sealed record SantriDashboardDto(
    DateTimeOffset GeneratedAtUtc,
    SantriDashboardProfileDto Profile,
    SantriDashboardHighlightDto Highlight,
    SantriDashboardOverviewDto Overview,
    SantriDashboardAttendanceDto Attendance,
    SantriDashboardKafarahDto Kafarah,
    SantriDashboardProgressDto Progress,
    SantriDashboardLogDto Logs);

public sealed record SantriDashboardProfileDto(
    Guid SantriId,
    string FullName,
    string Nis,
    string Kampus,
    string Jurusan,
    string Gender,
    string Tim,
    string Kelas,
    bool EmailConfirmed,
    string Role,
    string Source,
    bool IsSantriUser);

public sealed record SantriDashboardHighlightDto(
    int AttendancePercentage,
    int RemainingKafarah,
    int AverageProgressPercentage,
    int RecordedLogs);

public sealed record SantriDashboardOverviewDto(
    int TotalPresensi,
    int HadirCount,
    int TotalKafarah,
    int SisaKafarah,
    int TotalProgressEntries,
    int CompletedProgressEntries,
    int TotalLogs,
    int RecordedLogCount);

public sealed record SantriDashboardAttendanceDto(
    int Total,
    int Hadir,
    int Izin,
    int Sakit,
    int Alpha,
    int Persentase,
    IReadOnlyList<SantriDashboardAttendanceItemDto> Recent);

public sealed record SantriDashboardAttendanceItemDto(
    Guid Id,
    DateOnly Tanggal,
    string Nama,
    string KegiatanKategori,
    string Waktu,
    string Status,
    string? Catatan,
    DateTimeOffset CreatedAtUtc);

public sealed record SantriDashboardKafarahDto(
    int Total,
    int TotalKafarah,
    int JumlahSetor,
    int SisaTanggungan,
    IReadOnlyList<SantriDashboardKafarahItemDto> Recent);

public sealed record SantriDashboardKafarahItemDto(
    Guid Id,
    DateOnly Tanggal,
    string JenisPelanggaran,
    string JenisPelanggaranLabel,
    string Kafarah,
    int JumlahSetor,
    int Tanggungan,
    int SisaTanggungan,
    string? Tenggat);

public sealed record SantriDashboardProgressDto(
    int Total,
    int Completed,
    int InProgress,
    int Average,
    int Quran,
    int Hadits,
    IReadOnlyList<SantriDashboardProgressItemDto> Recent);

public sealed record SantriDashboardProgressItemDto(
    Guid Id,
    string Judul,
    int Target,
    int Capaian,
    string? Satuan,
    string? Level,
    int Persentase,
    string? Catatan,
    string? Pembimbing,
    DateTimeOffset? TerakhirSetorUtc,
    DateTimeOffset UpdatedAtUtc);

public sealed record SantriDashboardLogDto(
    int Total,
    int Tercatat,
    IReadOnlyList<SantriDashboardLogItemDto> Recent);

public sealed record SantriDashboardLogItemDto(
    Guid Id,
    DateOnly TanggalPengajuan,
    string Jenis,
    string? Rentang,
    string Status,
    string? Petugas,
    string? Catatan,
    DateTimeOffset CreatedAtUtc);
