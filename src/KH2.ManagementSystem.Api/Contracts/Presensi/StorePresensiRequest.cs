using System.ComponentModel.DataAnnotations;

namespace KH2.ManagementSystem.Api.Contracts.Presensi;

public sealed class StorePresensiRequest
{
    [Required]
    public Guid SantriId { get; init; }

    [Required]
    public DateOnly Tanggal { get; init; }

    [Required]
    public string Status { get; init; } = string.Empty;

    public string? Kegiatan { get; init; }

    public string? Keterangan { get; init; }

    public string? Waktu { get; init; }
}
