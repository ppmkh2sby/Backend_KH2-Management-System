using KH2.ManagementSystem.Domain.Common;

namespace KH2.ManagementSystem.Domain.Quran;

public sealed class QuranSurah : AuditableEntity<Guid>
{
    public QuranSurah(
        Guid id,
        int number,
        string name,
        string arabicName,
        int verseCount)
        : base(id)
    {
        Number = RequireSurahNumber(number);
        Name = RequireText(name, nameof(name));
        ArabicName = RequireText(arabicName, nameof(arabicName));
        VerseCount = RequireVerseCount(verseCount);
    }

    public int Number { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string ArabicName { get; private set; } = string.Empty;
    public int VerseCount { get; private set; }

    public void UpdateMetadata(
        string name,
        string arabicName,
        int verseCount,
        DateTimeOffset updatedAtUtc)
    {
        Name = RequireText(name, nameof(name));
        ArabicName = RequireText(arabicName, nameof(arabicName));
        VerseCount = RequireVerseCount(verseCount);
        Touch(updatedAtUtc);
    }

    private static int RequireSurahNumber(int value)
    {
        if (value is < 1 or > 114)
        {
            throw new ArgumentOutOfRangeException(nameof(value), "Surah number must be between 1 and 114.");
        }

        return value;
    }

    private static int RequireVerseCount(int value)
    {
        if (value <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(value), "Verse count must be greater than zero.");
        }

        return value;
    }

    private static string RequireText(string value, string paramName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException($"{paramName} is required.", paramName);
        }

        return value.Trim();
    }
}
