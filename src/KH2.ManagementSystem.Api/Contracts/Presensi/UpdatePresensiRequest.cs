namespace KH2.ManagementSystem.Api.Contracts.Presensi;

public sealed class UpdatePresensiRequest
{
    public Guid? SantriId { get; init; }
    public DateOnly? Tanggal { get; init; }
    public string? Status { get; init; }
    public string? Kegiatan { get; init; }
    public string? Keterangan { get; init; }
    public string? Waktu { get; init; }
}
