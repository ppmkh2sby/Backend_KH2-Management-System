using KH2.ManagementSystem.Domain.Quran;
using Xunit;

namespace KH2.ManagementSystem.UnitTests;

public sealed class QuranSurahTests
{
    [Fact]
    public void QuranSurahStoresItsMasterMetadata()
    {
        var surah = new QuranSurah(Guid.NewGuid(), 1, "Al-Fatihah", "الفاتحة", 7);

        Assert.Equal(1, surah.Number);
        Assert.Equal("Al-Fatihah", surah.Name);
        Assert.Equal("الفاتحة", surah.ArabicName);
        Assert.Equal(7, surah.VerseCount);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(115)]
    public void QuranSurahRejectsNumbersOutsideMushafRange(int number)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new QuranSurah(Guid.NewGuid(), number, "Test", "اختبار", 1));
    }
}
