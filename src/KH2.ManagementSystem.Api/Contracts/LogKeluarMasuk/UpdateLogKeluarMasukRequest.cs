namespace KH2.ManagementSystem.Api.Contracts.LogKeluarMasuk;

public sealed record UpdateLogKeluarMasukRequest(
    DateOnly Tanggal,
    string Tujuan,
    string WaktuKeluar,
    string WaktuMasuk,
    string? Catatan);
