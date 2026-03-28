namespace KH2.ManagementSystem.Api.Contracts.ProgressKeilmuan;

public sealed record SyncProgressKeilmuanRequest(
    string Category,
    IReadOnlyList<SyncProgressKeilmuanItemRequest> Modules);

public sealed record SyncProgressKeilmuanItemRequest(
    string Judul,
    int? Value);
