namespace KH2.ManagementSystem.Api.Contracts.LogKeluarMasuk;

public sealed record StoreLogKeluarMasukRequest(
    DateOnly Tanggal,
    string Tujuan,
    string WaktuKeluar,
    string WaktuMasuk,
    string? Catatan);
