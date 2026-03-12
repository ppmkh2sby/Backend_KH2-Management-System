using KH2.ManagementSystem.Domain.Common;

namespace KH2.ManagementSystem.Domain.Santris;

public sealed class Santri : AuditableEntity<Guid>
{
    public Santri(
        Guid id,
        Guid userId,
        string fullName,
        string nis,
        string kampus,
        string jurusan,
        string gender,
        string tim,
        string kelas,
        string? catatan = null)
        : base(id)
    {
        UserId = userId;
        FullName = Require(fullName, nameof(fullName));
        Nis = Require(nis, nameof(nis));
        Kampus = Require(kampus, nameof(kampus));
        Jurusan = Require(jurusan, nameof(jurusan));
        Gender = Require(gender, nameof(gender));
        Tim = Require(tim, nameof(tim));
        Kelas = Require(kelas, nameof(kelas));
        Catatan = catatan?.Trim();
    }

    public Guid UserId { get; private set; }
    public string FullName { get; private set; } = string.Empty;
    public string Nis { get; private set; } = string.Empty;
    public string Kampus { get; private set; } = string.Empty;
    public string Jurusan { get; private set; } = string.Empty;
    public string Gender { get; private set; } = string.Empty;
    public string Tim { get; private set; } = string.Empty;
    public string Kelas { get; private set; } = string.Empty;
    public string? Catatan { get; private set; }

    private static string Require(string value, string paramName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException($"{paramName} is required.", paramName);
        }

        return value.Trim();
    }
}