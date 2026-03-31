# Backend Deployment on Ubuntu 24.04

Dokumen ini menyiapkan backend ASP.NET Core agar dijalankan di Ubuntu 24.04 LTS dengan pola:

- backend berjalan sebagai service `systemd`
- backend berada di belakang Nginx reverse proxy
- frontend static dilayani Nginx pada `/`
- backend API diproxy Nginx pada `/api`
- PostgreSQL berjalan di server yang sama
- akses publik lewat Cloudflare Tunnel

## 1. Runtime dependencies

Project API ini menargetkan `net10.0`, jadi server butuh ASP.NET Core Runtime 10.0.

Untuk Ubuntu 24.04, gunakan feed Ubuntu atau Ubuntu .NET backports. Microsoft Learn menjelaskan bahwa mulai Ubuntu 24.04 paket .NET tidak lagi dipublikasikan ke Microsoft package repository, dan `aspnetcore-runtime-10.0` tersedia untuk Ubuntu 24.04 melalui feed Ubuntu/backports.

Contoh instalasi runtime:

```bash
sudo apt update
sudo apt install -y software-properties-common
sudo add-apt-repository ppa:dotnet/backports
sudo apt update
sudo apt install -y aspnetcore-runtime-10.0 nginx postgresql
```

Verifikasi:

```bash
dotnet --list-runtimes
psql --version
nginx -v
```

## 2. Publish release

Dari mesin build/development:

```powershell
./scripts/publish-linux-x64.ps1
```

Atau manual:

```powershell
dotnet publish src/KH2.ManagementSystem.Api/KH2.ManagementSystem.Api.csproj `
  -c Release `
  -r linux-x64 `
  --self-contained false `
  /p:UseAppHost=false `
  -o ./.codex-publish/backend/linux-x64
```

Hasil publish salin ke server, misalnya ke `/var/www/kh2-management-system/backend`.

## 3. Konfigurasi production

Gunakan salah satu dari dua pendekatan berikut:

- file `appsettings.Production.json` yang dibuat dari `src/KH2.ManagementSystem.Api/appsettings.Production.example.json`
- environment variables di unit `systemd`

Prioritas konfigurasi ASP.NET Core tetap berlaku, jadi environment variables akan mengoverride `appsettings`.

Default yang sekarang aman untuk production:

- migration aktif jika `Database__MigrateOnStartup` tidak diubah
- seeding non-sample tidak aktif kecuali `Database__SeedOnStartup=true`
- sample data tidak aktif kecuali `Database__SeedSampleDataOnStartup=true`

Jika frontend dan backend dilayani dari origin yang sama, `Cors__AllowedOrigins` boleh dibiarkan kosong. Jangan set `*`.

## 4. Environment variables penting

Minimal yang harus diisi di server:

```ini
ASPNETCORE_ENVIRONMENT=Production
ASPNETCORE_URLS=http://127.0.0.1:8080
ConnectionStrings__DefaultConnection=Host=127.0.0.1;Port=5432;Database=kh2_management_system;Username=kh2_app;Password=REPLACE_ME
Jwt__Issuer=https://api.example.com
Jwt__Audience=https://app.example.com
Jwt__SecretKey=REPLACE_WITH_A_LONG_RANDOM_SECRET_MIN_32_CHARS
Database__MigrateOnStartup=true
Database__SeedOnStartup=false
Database__SeedSampleDataOnStartup=false
AllowedHosts=api.example.com
```

Opsional:

```ini
Cors__AllowedOrigins__0=https://app.example.com
ReverseProxy__KnownProxies__0=127.0.0.1
ReverseProxy__KnownProxies__1=::1
ReverseProxy__ForwardLimit=1
```

Catatan:

- `ASPNETCORE_URLS` mengontrol bind host/port Kestrel. Untuk pola Nginx reverse proxy di server yang sama, gunakan loopback seperti `http://127.0.0.1:8080`.
- Jika connection string berisi karakter spesial dan dimasukkan ke unit `systemd`, gunakan `systemd-escape "<value>"` sebelum menyalinnya.

## 5. systemd service

Buat service file `/etc/systemd/system/kh2-management-system-backend.service`:

```ini
[Unit]
Description=KH2 Management System Backend
After=network.target postgresql.service
Wants=postgresql.service

[Service]
WorkingDirectory=/var/www/kh2-management-system/backend
ExecStart=/usr/bin/dotnet /var/www/kh2-management-system/backend/KH2.ManagementSystem.Api.dll
Restart=always
RestartSec=10
KillSignal=SIGINT
SyslogIdentifier=kh2-management-system-backend
User=www-data
Group=www-data
Environment=ASPNETCORE_ENVIRONMENT=Production
Environment=ASPNETCORE_URLS=http://127.0.0.1:8080
Environment=ConnectionStrings__DefaultConnection=Host=127.0.0.1;Port=5432;Database=kh2_management_system;Username=kh2_app;Password=REPLACE_ME
Environment=Jwt__Issuer=https://api.example.com
Environment=Jwt__Audience=https://app.example.com
Environment=Jwt__SecretKey=REPLACE_WITH_A_LONG_RANDOM_SECRET_MIN_32_CHARS
Environment=Database__MigrateOnStartup=true
Environment=Database__SeedOnStartup=false
Environment=Database__SeedSampleDataOnStartup=false
Environment=AllowedHosts=api.example.com
Environment=DOTNET_NOLOGO=true

[Install]
WantedBy=multi-user.target
```

Aktifkan:

```bash
sudo systemctl daemon-reload
sudo systemctl enable kh2-management-system-backend.service
sudo systemctl start kh2-management-system-backend.service
sudo systemctl status kh2-management-system-backend.service
sudo journalctl -fu kh2-management-system-backend.service
```

## 6. Nginx reverse proxy

Backend ini sekarang memproses `X-Forwarded-For`, `X-Forwarded-Proto`, dan `X-Forwarded-Host` dari proxy yang dipercaya. Karena itu:

- Nginx harus mengirim header forward yang benar
- backend sebaiknya hanya listen di loopback
- `UseHttpsRedirection()` akan membaca scheme yang sudah diforward dan tidak salah menganggap request public selalu `http`

Contoh konfigurasi Nginx:

```nginx
server {
    listen 80;
    server_name api.example.com;

    root /var/www/kh2-management-system/frontend;
    index index.html;

    location / {
        try_files $uri $uri/ /index.html;
    }

    location /api/ {
        proxy_pass http://127.0.0.1:8080;
        proxy_http_version 1.1;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
    }
}
```

Catatan penting:

- Gunakan `location /api/` agar route backend seperti `/api/v1/...` diteruskan apa adanya.
- Jika frontend dan backend berada pada origin yang sama, browser tidak membutuhkan CORS untuk request ke `/api`.
- Cloudflare Tunnel tetap menuju Nginx, bukan langsung ke Kestrel.

## 7. PostgreSQL

Checklist minimum:

```sql
CREATE ROLE kh2_app WITH LOGIN PASSWORD 'REPLACE_ME';
CREATE DATABASE kh2_management_system OWNER kh2_app;
```

Pastikan connection string production menunjuk ke database ini. Startup backend akan menjalankan migration jika `Database__MigrateOnStartup=true`.

## 8. Checklist verifikasi setelah deploy

```bash
curl -I http://127.0.0.1:8080/health
curl -I http://127.0.0.1/api/v1/system/info
sudo systemctl status kh2-management-system-backend.service
sudo journalctl -n 100 -u kh2-management-system-backend.service
sudo nginx -t
```

Verifikasi yang diharapkan:

- service backend `active (running)`
- `/health` merespons lewat Kestrel
- `/api/v1/...` merespons lewat Nginx
- tidak ada redirect loop saat akses melalui reverse proxy
- database migration sukses
- sample seed tidak berjalan kecuali diaktifkan eksplisit
