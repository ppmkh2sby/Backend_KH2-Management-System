namespace KH2.ManagementSystem.Api.Contracts.Santri;

public sealed record SantriResponse(
    Guid Id,
    Guid UserId,
    string Nama,
    string Nis,
    string Kampus,
    string Jurusan,
    string Gender,
    string Tim,
    string Kelas,
    string? Catatan);

public sealed record SantriListResponse(
    IReadOnlyList<SantriResponse> Items,
    int Page,
    int PerPage,
    int TotalCount);
