namespace KH2.ManagementSystem.Domain.Santris;

public static class SantriTeam
{
    public const string Ketertiban = "KTB";
    public const string Keilmuan = "KBM";

    public static string Normalize(string? team)
    {
        var normalized = new string((team ?? string.Empty)
            .Where(char.IsLetterOrDigit)
            .ToArray())
            .ToUpperInvariant();

        return normalized is "KETERTIBAN" or "TIMKETERTIBAN" or "TIMKTB"
            ? Ketertiban
            : normalized;
    }

    public static bool Is(string? actualTeam, string expectedTeam) =>
        string.Equals(
            Normalize(actualTeam),
            Normalize(expectedTeam),
            StringComparison.Ordinal);

    public static bool IsKetertiban(string? team) => Is(team, Ketertiban);
    public static bool IsKeilmuan(string? team) => Is(team, Keilmuan);
}
