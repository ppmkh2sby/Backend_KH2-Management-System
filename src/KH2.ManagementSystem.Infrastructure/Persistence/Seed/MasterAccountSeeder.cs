using KH2.ManagementSystem.Application.Abstractions.Security;
using KH2.ManagementSystem.Domain.Santris;
using KH2.ManagementSystem.Domain.Users;
using Microsoft.EntityFrameworkCore;

namespace KH2.ManagementSystem.Infrastructure.Persistence.Seed;

public sealed class MasterAccountSeeder(
    AppDbContext dbContext,
    IPasswordHasher passwordHasher)
{
    private const string InitialPassword = "Kh2Awal123!";

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        await SeedSantrisAsync(Putra(), cancellationToken);
        await SeedSantrisAsync(Putri(), cancellationToken);
        await SeedStaffAsync(DewanGuru(), cancellationToken);
        await SeedStaffAsync(Pengurus(), cancellationToken);

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task SeedSantrisAsync(
        IReadOnlyCollection<SantriSeedItem> items,
        CancellationToken cancellationToken)
    {
        foreach (var item in items)
        {
            var existingUser = await dbContext.Users
                .FirstOrDefaultAsync(x => x.Username == item.Nis, cancellationToken);

            if (existingUser is null)
            {
                var user = CreateUser(
                    username: item.Nis,
                    fullName: item.Name,
                    role: UserRole.Santri);

                await dbContext.Users.AddAsync(user, cancellationToken);
                existingUser = user;
            }

            var existingSantri = await dbContext.Santris
                .FirstOrDefaultAsync(x => x.Nis == item.Nis, cancellationToken);

            if (existingSantri is not null)
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
        CancellationToken cancellationToken)
    {
        foreach (var item in items)
        {
            var exists = await dbContext.Users
                .AnyAsync(x => x.Username == item.Code, cancellationToken);

            if (exists)
            {
                continue;
            }

            var user = CreateUser(
                username: item.Code,
                fullName: item.Name,
                role: item.Role);

            await dbContext.Users.AddAsync(user, cancellationToken);
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
            mustChangePassword: true);

        var hash = passwordHasher.HashPassword(user, InitialPassword);
        user.SetPasswordHash(hash);

        return user;
    }

    private static SantriSeedItem[] Putra() =>
        new[]
        {
            new SantriSeedItem("ABDULLAH JUWAN DAWATRA", "022121013", "UNAIR", "Keselamatan dan Kesehatan Kerja", "putra", "PH", "Cepatan"),
            new SantriSeedItem("MUHAMMAD FARID FATCHUR", "022222001", "ITS", "Sistem Perkapalan", "putra", "Kebersihan", "Cepatan"),
            new SantriSeedItem("MUHAMMAD FATI RAJINA NAFIE", "022222006", "UNAIR", "Matematika", "putra", "Sekben", "Lambatan"),
            new SantriSeedItem("ARDIAS AJI SAPUTRO", "022323002", "ITS", "Teknik Infrastruktur Sipil", "putra", "KBM", "Lambatan"),
            new SantriSeedItem("MUHAMMAD IRSYAD IBRAHIMOVIC", "022323004", "ITS", "Teknik Material dan Metalurgi", "putra", "PH", "Lambatan"),
            new SantriSeedItem("SYAIFUDIN AKBARI ABLUDIN", "022323006", "PPNS", "Manajemen Bisnis", "putra", "Acara", "Pegon"), // cek ejaan sesuai source asli
            new SantriSeedItem("ALVIDA RAHMAT", "022424001", "ITS", "Sistem Informasi", "putra", "KTB", "Lambatan"),
            new SantriSeedItem("FAHMI ROSYIDI AL ULYA", "022424006", "PENS", "Teknik Informatika", "putra", "Sekben", "Lambatan"),
            new SantriSeedItem("KEISHA ZAFIF FAHREZI", "022424007", "PENS", "Multimedia Broadcast", "putra", "Acara", "Lambatan"),
            new SantriSeedItem("MAESTRO RAFA AGNIYA", "022424008", "PENS", "Teknik Informatika", "putra", "KTB", "Lambatan"),
            new SantriSeedItem("MUHAMMAD FARREL AL-AQSA", "022424010", "UNAIR", "Fisioterapi", "putra", "Upkpt", "Bacaan"),
            new SantriSeedItem("MUHAMMAD FATIZKY ALFATH BAHARDAN PUTRA", "022424011", "ITS", "Teknik Sipil", "putra", "KBM", "Pegon"), // cek ejaan sesuai source asli
            new SantriSeedItem("MUHAMMAD SETYO ARFAN IBRAHIM", "022424012", "ITS", "Desain Produk", "putra", "Upkpt", "Bacaan"),
            new SantriSeedItem("VIKY KARUNIA PUTRA PRATAMA", "022423017", "PPNS", "Teknik Desain dan Manufaktur", "putra", "Kebersihan", "Lambatan"),
            new SantriSeedItem("ZAKI AFIF ARIF", "022424019", "ITS", "Teknik Lingkungan", "putra", "KBM", "Bacaan"),
            new SantriSeedItem("BRILIANT AHMAD RAMADHAN", "022525004", "ITS", "Teknik Lepas Pantai", "putra", "Kebersihan", "Lambatan"),
            new SantriSeedItem("DIMAS ADI SANJAYA", "022525005", "Universitas Dr. Soetomo", "Manajemen", "putra", "Upkpt", "Bacaan"),
            new SantriSeedItem("FARIS JULDAN", "022525006", "PPNS", "Keselamatan dan Kesehatan Kerja", "putra", "Sekben", "Bacaan"),
            new SantriSeedItem("HANAFI SATRIYO UTOMO SETIAWAN", "022525007", "ITS", "S2 - Teknik Informatika", "putra", "Acara", "Bacaan"),
            new SantriSeedItem("SOFWAN IFTAKHUDDIN MAARIF", "022525013", "UNAIR", "Farmasi", "putra", "KTB", "Bacaan"),
            new SantriSeedItem("MUHAMAD BAEHAQI AL MUJAHIDIN", "022524015", "UNAIR", "Teknologi Hasil Perikanan", "putra", "Acara", "Pegon")
        };

    private static SantriSeedItem[] Putri() =>
        new[]
        {
            new SantriSeedItem("TARTISA ADELYA SAFETRA", "022222004", "ITS", "Perencanaan Wilayah dan Kota", "putri", "Acara", "Cepatan"),
            new SantriSeedItem("AISYA WIDYA PRATIWI", "022323001", "UNAIR", "Matematika", "putri", "PH", "Cepatan"),
            new SantriSeedItem("CASEY PALLAS TALITHA HARJANTO", "022323003", "ITS", "Desain Komunikasi Visual", "putri", "Kebersihan", "Lambatan"),
            new SantriSeedItem("RIZKY KHOIRUNNISA", "022323005", "PENS", "Teknik Telekomunikasi", "putri", "KBM", "Pegon"),
            new SantriSeedItem("AYESHA NAYYARA PUTRI MURODI", "022424002", "PPNS", "Teknik Perancangan dan Konstruksi Kapal", "putri", "Acara", "Lambatan"),
            new SantriSeedItem("AZZAHRA JAMALULLAILY MAFZA", "022424003", "UNAIR", "Bahasa dan Sastra Inggris", "putri", "Upkpt", "Lambatan"), // cek ejaan
            new SantriSeedItem("CHERFINE AN-NISAUL AULIYA ULLA", "022424004", "ITS", "Teknik Sipil", "putri", "KBM", "Lambatan"), // cek ejaan
            new SantriSeedItem("DEVEN KARTIKA WITAYA", "022424005", "ITS", "Arsitektur", "putri", "Kebersihan", "Lambatan"), // cek ejaan
            new SantriSeedItem("MARITZA DARA ATHIFA", "022424009", "ITS", "Sistem Informasi", "putri", "Sekben", "Pegon"),
            new SantriSeedItem("NABILA KAYSA ADRISTI", "022424013", "ITS", "Studi Pembangunan", "putri", "Kebersihan", "Cepatan"),
            new SantriSeedItem("RARA ARIMBI GITA ATMODJO", "022424014", "ITS", "Desain Komunikasi Visual", "putri", "PH", "Pegon"),
            new SantriSeedItem("RENATA KEYSHA AZALIA KHORUNNISA", "022424015", "ITS", "Teknik Geofisika", "putri", "Acara", "Lambatan"),
            new SantriSeedItem("SYAHDINDA SHERLYA LAURA", "022424016", "UNAIR", "Bahasa dan Sastra Inggris", "putri", "KTB", "Lambatan"),
            new SantriSeedItem("ZAHRA SUCIANA TRI AMMA MARETHA", "022424018", "UNAIR", "Akuntansi", "putri", "Sekben", "Lambatan"),
            new SantriSeedItem("AMANDA RAMADANI PUTRI PANGESTI", "022525001", "PENS", "Teknik Informatika", "putri", "Upkpt", "Bacaan"),
            new SantriSeedItem("AURA RENATA ADASIYA AZKA", "022525002", "PENS", "Sains Data Terapan", "putri", "Acara", "Bacaan"),
            new SantriSeedItem("BALQIS SALWA AURELIA AZZAHRA", "022525003", "ITS", "Teknologi Kedokteran", "putri", "KTB", "Bacaan"),
            new SantriSeedItem("IMELYA URIVARTOUSTI", "022525008", "ITS", "Sistem Informasi", "putri", "KTB", "Lambatan"), // cek ejaan + NIS
            new SantriSeedItem("MAYLAVASA ADIVA BIQIS", "022525009", "PENS", "Teknik Elektronika Industri", "putri", "Kebersihan", "Bacaan"), // cek ejaan
            new SantriSeedItem("QISTHI KHIROFATI MADINA SENOA", "022525010", "PENS", "Teknik Informatika", "putri", "Acara", "Bacaan"), // cek ejaan
            new SantriSeedItem("RASHIDA ZARA FAUZIAH", "022525013", "ITS", "Studi Pembangunan", "putri", "Sekben", "Lambatan"),
            new SantriSeedItem("SAFA KARINDAH KAHYA AISHA", "022525012", "UMS", "Farmasi", "putri", "Upkpt", "Bacaan"), // cek ejaan
            new SantriSeedItem("SYARIFAH HURI FILJANNAH", "022525014", "ITS", "Teknik Kimia", "putri", "KBM", "Bacaan")
        };

    private static StaffSeedItem[] DewanGuru() =>
        new[]
        {
            new StaffSeedItem("0235499001", "Amir", UserRole.DewanGuru),
            new StaffSeedItem("0235499002", "Anton", UserRole.DewanGuru),
            new StaffSeedItem("0235499003", "Ridho", UserRole.DewanGuru)
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
}
