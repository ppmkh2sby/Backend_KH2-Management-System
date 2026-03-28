using System.Globalization;
using KH2.ManagementSystem.Application.Abstractions.Security;
using KH2.ManagementSystem.Domain.Kafarahs;
using KH2.ManagementSystem.Domain.Kegiatans;
using KH2.ManagementSystem.Domain.LogKeluarMasuks;
using KH2.ManagementSystem.Domain.Presensis;
using KH2.ManagementSystem.Domain.ProgressKeilmuans;
using KH2.ManagementSystem.Domain.Santris;
using KH2.ManagementSystem.Domain.Sesis;
using KH2.ManagementSystem.Domain.Users;
using KH2.ManagementSystem.Domain.Walis;
using Microsoft.EntityFrameworkCore;

namespace KH2.ManagementSystem.Infrastructure.Persistence.Seed;

public sealed class MasterAccountSeeder(
    AppDbContext dbContext,
    IPasswordHasher passwordHasher)
{
    private const string InitialPassword = "Kh2Awal123!";

    public async Task SeedAsync(
        bool includeSampleData = true,
        CancellationToken cancellationToken = default)
    {
        var knownUsernames = new HashSet<string>(
            await dbContext.Users
                .AsNoTracking()
                .Select(x => x.Username)
                .ToListAsync(cancellationToken),
            StringComparer.Ordinal);

        var knownSantriNis = new HashSet<string>(
            await dbContext.Santris
                .AsNoTracking()
                .Select(x => x.Nis)
                .ToListAsync(cancellationToken),
            StringComparer.Ordinal);

        await SeedSantrisAsync(Putra(), knownUsernames, knownSantriNis, cancellationToken);
        await SeedSantrisAsync(Putri(), knownUsernames, knownSantriNis, cancellationToken);
        await SeedStaffAsync(Admins(), knownUsernames, cancellationToken);
        await SeedStaffAsync(DewanGuru(), knownUsernames, cancellationToken);
        await SeedStaffAsync(Pengurus(), knownUsernames, cancellationToken);
        await SeedStaffAsync(WaliSantris(), knownUsernames, cancellationToken);

        await dbContext.SaveChangesAsync(cancellationToken);
        await SeedRelatedDataAsync(includeSampleData, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task SeedSantrisAsync(
        IReadOnlyCollection<SantriSeedItem> items,
        HashSet<string> knownUsernames,
        HashSet<string> knownSantriNis,
        CancellationToken cancellationToken)
    {
        foreach (var item in items)
        {
            var existingUser = dbContext.Users.Local
                .FirstOrDefault(x => x.Username == item.Nis)
                ?? await dbContext.Users
                    .FirstOrDefaultAsync(x => x.Username == item.Nis, cancellationToken);

            if (existingUser is null && knownUsernames.Add(item.Nis))
            {
                var user = CreateUser(
                    username: item.Nis,
                    fullName: item.Name,
                    role: UserRole.Santri);

                await dbContext.Users.AddAsync(user, cancellationToken);
                existingUser = user;
            }

            if (existingUser is not null)
            {
                existingUser.Rename(item.Name);
                existingUser.Activate();
                existingUser.CompletePasswordChange();

                EnsureSeedPassword(existingUser);
            }

            if (existingUser is null || !knownSantriNis.Add(item.Nis))
            {
                continue;
            }

            var santri = new Santri(
                Guid.NewGuid(),
                existingUser.Id,
                item.Name,
                item.Nis,
                item.Kampus,
                item.Jurusan,
                item.Gender,
                item.Tim,
                item.Kelas,
                item.Catatan);

            await dbContext.Santris.AddAsync(santri, cancellationToken);
        }
    }

    private async Task SeedStaffAsync(
        IReadOnlyCollection<StaffSeedItem> items,
        HashSet<string> knownUsernames,
        CancellationToken cancellationToken)
    {
        foreach (var item in items)
        {
            var existingUser = dbContext.Users.Local
                .FirstOrDefault(x => x.Username == item.Code)
                ?? await dbContext.Users
                    .FirstOrDefaultAsync(x => x.Username == item.Code, cancellationToken);

            if (existingUser is not null)
            {
                existingUser.Rename(item.Name);
                existingUser.Activate();
                existingUser.CompletePasswordChange();
                EnsureSeedPassword(existingUser);

                continue;
            }

            if (!knownUsernames.Add(item.Code))
            {
                continue;
            }

            var user = CreateUser(
                username: item.Code,
                fullName: item.Name,
                role: item.Role);

            user.CompletePasswordChange();
            await dbContext.Users.AddAsync(user, cancellationToken);
        }
    }

    private async Task SeedRelatedDataAsync(
        bool includeSampleData,
        CancellationToken cancellationToken)
    {
        var santris = await dbContext.Santris
            .AsNoTracking()
            .OrderBy(x => x.Gender)
            .ThenBy(x => x.Nis)
            .ToListAsync(cancellationToken);

        if (santris.Count == 0)
        {
            return;
        }

        var santriByNis = santris.ToDictionary(x => x.Nis, StringComparer.Ordinal);

        await SeedWaliRelationsAsync(santriByNis, cancellationToken);

        if (!includeSampleData)
        {
            return;
        }

        var sessions = await EnsureSeedSessionsAsync(cancellationToken);
        await SeedPresensiAsync(santris, sessions, cancellationToken);
        await SeedKafarahAsync(santris, cancellationToken);
        await SeedProgressAsync(santris, cancellationToken);
        await SeedLogKeluarMasukAsync(santris, cancellationToken);
    }

    private async Task SeedWaliRelationsAsync(
        Dictionary<string, Santri> santriByNis,
        CancellationToken cancellationToken)
    {
        var relationSpecs = new[]
        {
            new { Username = "wali", SantriNis = new[] { "022424008", "022424001" } },
            new { Username = "wali-putri", SantriNis = new[] { "022424016", "022525003" } }
        };

        foreach (var spec in relationSpecs)
        {
            var waliUser = await dbContext.Users
                .FirstOrDefaultAsync(
                    x => x.Username == spec.Username && x.Role == UserRole.WaliSantri,
                    cancellationToken);

            if (waliUser is null)
            {
                continue;
            }

            foreach (var nis in spec.SantriNis)
            {
                if (!santriByNis.TryGetValue(nis, out var santri))
                {
                    continue;
                }

                var existing = await dbContext.WaliSantriRelations
                    .FirstOrDefaultAsync(
                        x => x.WaliUserId == waliUser.Id && x.SantriId == santri.Id,
                        cancellationToken);

                if (existing is not null)
                {
                    existing.ChangeRelationshipLabel("Orang Tua");
                    continue;
                }

                await dbContext.WaliSantriRelations.AddAsync(
                    new WaliSantriRelation(
                        Guid.NewGuid(),
                        waliUser.Id,
                        santri.Id,
                        "Orang Tua"),
                    cancellationToken);
            }
        }
    }

    private async Task<IReadOnlyList<(Kegiatan Kegiatan, Sesi Sesi)>> EnsureSeedSessionsAsync(
        CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow.Date);
        var specs = new[]
        {
            new SessionSeedItem(today.AddDays(-3), "sambung", "pagi", "Sambung Pagi"),
            new SessionSeedItem(today.AddDays(-2), "asrama", "subuh", "Asrama Subuh"),
            new SessionSeedItem(today.AddDays(-1), "sambung", "siang", "Sambung Siang"),
            new SessionSeedItem(today, "asrama", "malam", "Asrama Malam")
        };

        var result = new List<(Kegiatan Kegiatan, Sesi Sesi)>(specs.Length);
        foreach (var spec in specs)
        {
            var kegiatan = await EnsureKegiatanAsync(spec.Kategori, spec.Waktu, spec.Catatan, cancellationToken);
            var sesi = await EnsureSesiAsync(kegiatan.Id, spec.Tanggal, cancellationToken);
            result.Add((kegiatan, sesi));
        }

        return result;
    }

    private async Task<Kegiatan> EnsureKegiatanAsync(
        string kategori,
        string waktu,
        string catatan,
        CancellationToken cancellationToken)
    {
        var existing = dbContext.Kegiatans.Local
            .FirstOrDefault(x => x.Kategori == kategori && x.Waktu == waktu)
            ?? await dbContext.Kegiatans
                .FirstOrDefaultAsync(x => x.Kategori == kategori && x.Waktu == waktu, cancellationToken);

        if (existing is not null)
        {
            return existing;
        }

        var kegiatan = new Kegiatan(Guid.NewGuid(), kategori, waktu, catatan);
        await dbContext.Kegiatans.AddAsync(kegiatan, cancellationToken);
        return kegiatan;
    }

    private async Task<Sesi> EnsureSesiAsync(
        Guid kegiatanId,
        DateOnly tanggal,
        CancellationToken cancellationToken)
    {
        var existing = dbContext.Sesis.Local
            .FirstOrDefault(x => x.KegiatanId == kegiatanId && x.Tanggal == tanggal)
            ?? await dbContext.Sesis
                .FirstOrDefaultAsync(x => x.KegiatanId == kegiatanId && x.Tanggal == tanggal, cancellationToken);

        if (existing is not null)
        {
            return existing;
        }

        var sesi = new Sesi(Guid.NewGuid(), kegiatanId, tanggal);
        await dbContext.Sesis.AddAsync(sesi, cancellationToken);
        return sesi;
    }

    private async Task SeedPresensiAsync(
        List<Santri> santris,
        IReadOnlyList<(Kegiatan Kegiatan, Sesi Sesi)> sessions,
        CancellationToken cancellationToken)
    {
        for (var santriIndex = 0; santriIndex < santris.Count; santriIndex++)
        {
            var santri = santris[santriIndex];

            for (var sessionIndex = 0; sessionIndex < sessions.Count; sessionIndex++)
            {
                var session = sessions[sessionIndex];
                var status = ResolvePresensiStatus(santriIndex, sessionIndex);
                var catatan = ResolvePresensiCatatan(status);

                var existing = await dbContext.Presensis
                    .FirstOrDefaultAsync(
                        x => x.SantriId == santri.Id && x.SesiId == session.Sesi.Id,
                        cancellationToken);

                if (existing is not null)
                {
                    existing.Update(
                        santri.Id,
                        santri.FullName,
                        status,
                        session.Kegiatan.Id,
                        session.Sesi.Id,
                        catatan,
                        session.Kegiatan.Waktu,
                        DateTimeOffset.UtcNow);

                    continue;
                }

                await dbContext.Presensis.AddAsync(
                    new Presensi(
                        Guid.NewGuid(),
                        santri.Id,
                        santri.FullName,
                        status,
                        session.Kegiatan.Id,
                        session.Sesi.Id,
                        catatan,
                        session.Kegiatan.Waktu),
                    cancellationToken);
            }
        }
    }

    private async Task SeedKafarahAsync(
        List<Santri> santris,
        CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow.Date);
        var definitions = new[]
        {
            "tidak_sholat_subuh_di_masjid",
            "tidak_sambung_pagi",
            "tidak_sambung_malam",
            "tidak_apel_malam",
            "tidak_sholat_malam",
            "terlambat_kembali_ke_ppm",
            "tidak_asrama_sesi_pagi",
            "tidak_asrama_sesi_siang",
            "tidak_asrama_sesi_sore",
            "tidak_asrama_sesi_malam"
        };

        for (var index = 0; index < Math.Min(10, santris.Count); index++)
        {
            var santri = santris[index];
            var jenis = definitions[index % definitions.Length];
            var definition = Kafarah.ResolveDefinition(jenis);
            var tanggal = today.AddDays(-(index + 1));
            var jumlahSetor = index % 3;
            var tanggungan = Math.Max(1, definition.DefaultAmount);
            var tenggat = tanggal.AddDays(7).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

            var existing = await dbContext.Kafarahs
                .FirstOrDefaultAsync(
                    x => x.SantriId == santri.Id &&
                        x.Tanggal == tanggal &&
                        x.JenisPelanggaran == jenis,
                    cancellationToken);

            if (existing is not null)
            {
                existing.Update(
                    tanggal,
                    definition.Code,
                    definition.Description,
                    jumlahSetor,
                    tanggungan,
                    tenggat,
                    DateTimeOffset.UtcNow);

                continue;
            }

            await dbContext.Kafarahs.AddAsync(
                new Kafarah(
                    Guid.NewGuid(),
                    santri.Id,
                    tanggal,
                    definition.Code,
                    definition.Description,
                    jumlahSetor,
                    tanggungan,
                    tenggat),
                cancellationToken);
        }
    }

    private async Task SeedProgressAsync(
        List<Santri> santris,
        CancellationToken cancellationToken)
    {
        for (var index = 0; index < Math.Min(12, santris.Count); index++)
        {
            var santri = santris[index];
            var progressSpecs = new[]
            {
                new
                {
                    Judul = "Setor Juz 30",
                    Target = 37,
                    Capaian = Math.Min(37, 18 + index),
                    Satuan = "halaman",
                    Level = ProgressKeilmuan.LevelQuran,
                    Catatan = "Perkembangan murojaah mingguan",
                    Pembimbing = "Ustadz Amir"
                },
                new
                {
                    Judul = "Hafalan Arbain",
                    Target = 20,
                    Capaian = Math.Min(20, 8 + (index % 10)),
                    Satuan = "hadits",
                    Level = ProgressKeilmuan.LevelHadits,
                    Catatan = "Setoran hadits pekanan",
                    Pembimbing = "Ustadz Anton"
                }
            };

            foreach (var spec in progressSpecs)
            {
                var existing = await dbContext.ProgressKeilmuans
                    .AnyAsync(
                        x => x.SantriId == santri.Id && x.Judul == spec.Judul,
                        cancellationToken);

                if (existing)
                {
                    continue;
                }

                await dbContext.ProgressKeilmuans.AddAsync(
                    new ProgressKeilmuan(
                        Guid.NewGuid(),
                        santri.Id,
                        spec.Judul,
                        spec.Target,
                        spec.Capaian,
                        spec.Satuan,
                        spec.Level,
                        spec.Catatan,
                        spec.Pembimbing,
                        DateTimeOffset.UtcNow.AddDays(-(index + 1))),
                    cancellationToken);
            }
        }
    }

    private async Task SeedLogKeluarMasukAsync(
        List<Santri> santris,
        CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow.Date);

        for (var index = 0; index < Math.Min(8, santris.Count); index++)
        {
            var santri = santris[index];
            var tanggal = today.AddDays(-(index + 1));
            var jenis = index % 2 == 0 ? "Keluar" : "Masuk";
            var status = (index % 3) switch
            {
                0 => LogKeluarMasuk.StatusRecorded,
                1 => LogKeluarMasuk.StatusPending,
                _ => LogKeluarMasuk.StatusApproved
            };

            var exists = await dbContext.LogKeluarMasuks
                .AnyAsync(
                    x => x.SantriId == santri.Id &&
                        x.TanggalPengajuan == tanggal &&
                        x.Jenis == jenis,
                    cancellationToken);

            if (exists)
            {
                continue;
            }

            await dbContext.LogKeluarMasuks.AddAsync(
                new LogKeluarMasuk(
                    Guid.NewGuid(),
                    santri.Id,
                    tanggal,
                    jenis,
                    "14.00 - 16.00",
                    status,
                    "Ketertiban",
                    index % 2 == 0 ? "Izin kegiatan kampus." : "Kembali ke pondok."),
                cancellationToken);
        }
    }

    private User CreateUser(string username, string fullName, UserRole role)
    {
        var user = new User(
            Guid.NewGuid(),
            username: username,
            fullName: fullName,
            email: null,
            role: role,
            passwordHash: "TEMP_HASH",
            emailConfirmed: false,
            isActive: true,
            mustChangePassword: false);

        var hash = passwordHasher.HashPassword(user, InitialPassword);
        user.SetPasswordHash(hash);

        return user;
    }

    private void EnsureSeedPassword(User user)
    {
        if (passwordHasher.VerifyPassword(user, user.PasswordHash, InitialPassword))
        {
            return;
        }

        var hash = passwordHasher.HashPassword(user, InitialPassword);
        user.SetPasswordHash(hash);
    }

    private static SantriSeedItem[] Putra() =>
        new[]
        {
            new SantriSeedItem("ABDULLAH JUWAN DAWAIRA", "022121013", "UNAIR", "Keselamatan dan Kesehatan Kerja", "putra", "PH", "Cepatan"),
            new SantriSeedItem("MUHAMMAD FARID FATCHUR", "022222001", "ITS", "Sistem Perkapalan", "putra", "Kebersihan", "Cepatan"),
            new SantriSeedItem("MUHAMMAD FATH RAJIHAN NAFIE", "022222006", "UNAIR", "Matematika", "putra", "Sekben", "Lambatan"),
            new SantriSeedItem("ARDIAS AJI SAPUTRO", "022323002", "ITS", "Teknik Infrastruktur Sipil", "putra", "KBM", "Lambatan"),
            new SantriSeedItem("MUHAMMAD IRSYAD IBRAHIMOVIC", "022323004", "ITS", "Teknik Material dan Metalurgi", "putra", "PH", "Lambatan"),
            new SantriSeedItem("SYAIFUDIN AKBARI ABILUDIN", "022323006", "PPNS", "Manajemen Bisnis", "putra", "Acara", "Pegon"), 
            new SantriSeedItem("ALWIDA RAHMAT", "022424001", "ITS", "Sistem Informasi", "putra", "KTB", "Lambatan"),
            new SantriSeedItem("FAHMI ROSYIDIN AL'ULYA", "022424006", "PENS", "Teknik Informatika", "putra", "Sekben", "Lambatan"),
            new SantriSeedItem("KEISHA ZAFIF FAHREZI", "022424007", "PENS", "Multimedia Broadcast", "putra", "Acara", "Lambatan"),
            new SantriSeedItem("MAESTRO RAFA AGNIYA", "022424008", "PENS", "Teknik Informatika", "putra", "KTB", "Lambatan"),
            new SantriSeedItem("MUHAMMAD FARREL AL-AQSA", "022424010", "UNAIR", "Fisioterapi", "putra", "Upkpt", "Bacaan"),
            new SantriSeedItem("MUHAMMAD FARIZKY ALFATH MAHARDIAN PUTRA", "022424011", "ITS", "Teknik Sipil", "putra", "KBM", "Pegon"), 
            new SantriSeedItem("MUHAMMAD SETYO ARFAN IBRAHIM", "022424012", "ITS", "Desain Produk", "putra", "Upkpt", "Bacaan"),
            new SantriSeedItem("VIKY KARUNIA PUTRA PRATAMA", "022423017", "PPNS", "Teknik Desain dan Manufaktur", "putra", "Kebersihan", "Lambatan"),
            new SantriSeedItem("ZAKI AFIF ARIF", "022424019", "ITS", "Teknik Lingkungan", "putra", "KBM", "Bacaan"),
            new SantriSeedItem("BRILIANT ACHMAD RAMADHAN", "022525004", "ITS", "Teknik Lepas Pantai", "putra", "Kebersihan", "Lambatan"),
            new SantriSeedItem("DIMAS ADI SANJAYA", "022525005", "Universitas Dr. Soetomo", "Manajemen", "putra", "Upkpt", "Bacaan"),
            new SantriSeedItem("FARIS JULDAN", "022525006", "PPNS", "Keselamatan dan Kesehatan Kerja", "putra", "Sekben", "Bacaan"),
            new SantriSeedItem("HANAFI SATRIYO UTOMO SETIAWAN", "022525007", "ITS", "S2 - Teknik Informatika", "putra", "Acara", "Bacaan"),
            new SantriSeedItem("SOFWAN MIFTAKHUDDIN MAARIF", "022525013", "UNAIR", "Farmasi", "putra", "KTB", "Bacaan"),
            new SantriSeedItem("MUHAMAD BAEHAQI AL MUJAHIDIN", "022524015", "UNAIR", "Teknologi Hasil Perikanan", "putra", "Acara", "Pegon")
        };

    private static SantriSeedItem[] Putri() =>
        new[]
        {
            new SantriSeedItem("TARISSA ADELYA SAFIERA", "022222004", "ITS", "Perencanaan Wilayah dan Kota", "putri", "Acara", "Cepatan"),
            new SantriSeedItem("AISYA WIDYA PRATIWI", "022323001", "UNAIR", "Matematika", "putri", "PH", "Cepatan"),
            new SantriSeedItem("CASEY PALLAS TALITHA HARJANTO", "022323003", "ITS", "Desain Komunikasi Visual", "putri", "Kebersihan", "Lambatan"),
            new SantriSeedItem("RIZKY KHOIRUNNISA", "022323005", "PENS", "Teknik Telekomunikasi", "putri", "KBM", "Pegon"),
            new SantriSeedItem("AYESHA NAYYARA PUTRI WURYADI", "022424002", "PPNS", "Teknik Perancangan dan Konstruksi Kapal", "putri", "Acara", "Lambatan"),
            new SantriSeedItem("AZZAHRA JAMALULLAILY MAFZA", "022424003", "UNAIR", "Bahasa dan Sastra Inggris", "putri", "Upkpt", "Lambatan"), 
            new SantriSeedItem("CHERFINE AN-NISAUL AULIYA ULLA", "022424004", "ITS", "Teknik Sipil", "putri", "KBM", "Lambatan"), 
            new SantriSeedItem("DEVEN KARTIKA WIJAYA", "022424005", "ITS", "Arsitektur", "putri", "Kebersihan", "Lambatan"),
            new SantriSeedItem("MARITZA DARA ATHIFA", "022424009", "ITS", "Sistem Informasi", "putri", "Sekben", "Pegon"),
            new SantriSeedItem("NABILA KAYSA ADRISTI", "022424013", "ITS", "Studi Pembangunan", "putri", "Kebersihan", "Cepatan"),
            new SantriSeedItem("RARA ARIMBI GITA ATMODJO", "022424014", "ITS", "Desain Komunikasi Visual", "putri", "PH", "Pegon"),
            new SantriSeedItem("RENATA KEYSHA AZALIA KHORUNNISA", "022424015", "ITS", "Teknik Geofisika", "putri", "Acara", "Lambatan"),
            new SantriSeedItem("SYAHDINDA SHERLYTA LAURA", "022424016", "UNAIR", "Bahasa dan Sastra Inggris", "putri", "KTB", "Lambatan"),
            new SantriSeedItem("ZAHRA SUCIANA TRI AMMA MARETHA", "022424018", "UNAIR", "Akuntansi", "putri", "Sekben", "Lambatan"),
            new SantriSeedItem("AMANDA RAMADHANI PUTRI PANGESTI", "022525001", "PENS", "Teknik Informatika", "putri", "Upkpt", "Bacaan"),
            new SantriSeedItem("AURA RENATA ANASYIYA AZKA", "022525002", "PENS", "Sains Data Terapan", "putri", "Acara", "Bacaan"),
            new SantriSeedItem("BALQIS SALWA AURELIA AZZAHRA", "022525003", "ITS", "Teknologi Kedokteran", "putri", "KTB", "Bacaan"),
            new SantriSeedItem("IMELYA URIVARTOUSI", "022525008", "ITS", "Sistem Informasi", "putri", "KTB", "Lambatan"), 
            new SantriSeedItem("MAYLAVASA ADIVA BILQIS", "022525009", "PENS", "Teknik Elektronika Industri", "putri", "Kebersihan", "Bacaan"), 
            new SantriSeedItem("QISTHI KHIROFATI MADINA SENOAJI", "022525010", "PENS", "Teknik Informatika", "putri", "Acara", "Bacaan"), 
            new SantriSeedItem("RASHIDA ZARA FAUZIAH", "022525011", "ITS", "Studi Pembangunan", "putri", "Sekben", "Lambatan"),
            new SantriSeedItem("SAFA KARINDAH KAHAYA AISHA", "022525012", "UMS", "Farmasi", "putri", "Upkpt", "Bacaan"),
            new SantriSeedItem("SYARIFAH HUURI FILJANNAH", "022525014", "ITS", "Teknik Kimia", "putri", "KBM", "Bacaan")
        };

    private static StaffSeedItem[] DewanGuru() =>
        new[]
        {
            new StaffSeedItem("0235499001", "Amir", UserRole.DewanGuru),
            new StaffSeedItem("0235499002", "Anton", UserRole.DewanGuru),
            new StaffSeedItem("0235499003", "Ridho", UserRole.DewanGuru)
        };

    private static StaffSeedItem[] Admins() =>
        new[]
        {
            new StaffSeedItem("admin", "Admin KH2", UserRole.Admin)
        };

    private static StaffSeedItem[] Pengurus() =>
        new[]
        {
            new StaffSeedItem("0218354001", "Saiful", UserRole.Pengurus),
            new StaffSeedItem("0218354002", "Hirul", UserRole.Pengurus),
            new StaffSeedItem("0218354003", "Angga", UserRole.Pengurus),
            new StaffSeedItem("0218354004", "Avan", UserRole.Pengurus),
            new StaffSeedItem("0218354005", "Abdurrahman", UserRole.Pengurus)
        };

    private static StaffSeedItem[] WaliSantris() =>
        new[]
        {
            new StaffSeedItem("wali", "Wali Santri KH2", UserRole.WaliSantri),
            new StaffSeedItem("wali-putri", "Wali Santri Putri KH2", UserRole.WaliSantri)
        };

    private static string ResolvePresensiStatus(int santriIndex, int sessionIndex)
    {
        return ((santriIndex + sessionIndex) % 10) switch
        {
            0 => "sakit",
            1 => "izin",
            2 => "alpa",
            3 => "izin",
            _ => "hadir"
        };
    }

    private static string? ResolvePresensiCatatan(string status)
    {
        return status switch
        {
            "izin" => "Izin kegiatan kampus.",
            "sakit" => "Sedang kurang fit.",
            "alpa" => "Belum ada keterangan.",
            _ => "Hadir sesuai jadwal."
        };
    }

    private sealed record SantriSeedItem(
        string Name,
        string Nis,
        string Kampus,
        string Jurusan,
        string Gender,
        string Tim,
        string Kelas,
        string? Catatan = null);

    private sealed record StaffSeedItem(
        string Code,
        string Name,
        UserRole Role);

    private sealed record SessionSeedItem(
        DateOnly Tanggal,
        string Kategori,
        string Waktu,
        string Catatan);
}
