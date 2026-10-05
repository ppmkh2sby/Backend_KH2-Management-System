using KH2.ManagementSystem.Domain.JurnalKeilmuans;
using Xunit;

namespace KH2.ManagementSystem.UnitTests;

public sealed class JurnalKeilmuanTests
{
    [Fact]
    public void JurnalStoresQuranTargetAndRealization()
    {
        var targetSurahId = Guid.NewGuid();
        var realizationSurahId = Guid.NewGuid();
        var journal = new JurnalKeilmuan(
            Guid.NewGuid(),
            new JurnalKeilmuanData(
                new DateOnly(2026, 8, 21), "SUBUH", "LAMBATAN", "UST. AMIR", 1m, true,
                "AL-QURAN", Guid.NewGuid(), "Anggota KBM", targetSurahId, 1, 7,
                realizationSurahId, 7, "Penuh", "{}"));

        Assert.Equal(targetSurahId, journal.QuranTargetSurahId);
        Assert.Equal(1, journal.QuranAyatAwal);
        Assert.Equal(7, journal.QuranAyatTarget);
        Assert.Equal(realizationSurahId, journal.QuranRealisasiSurahId);
        Assert.Equal(7, journal.QuranAyatRealisasi);
    }
}
