# Frontend deployment on Ubuntu 24.04

Frontend berada di folder `frontend` dan dibangun sebagai static single-page application (SPA). Dalam production, aplikasi tidak perlu Node.js berjalan: hasil build berupa file statis yang dilayani Nginx. Konfigurasi Nginx juga meneruskan `/health` dan `/api/` ke backend.

## Koneksi dengan backend

Konfigurasi default `VITE_API_BASE_URL=/api/v1` berarti browser memanggil endpoint relatif seperti `/api/v1/auth/login`. Nginx meneruskan semua request `/api/` ke Kestrel pada `127.0.0.1:8080`.

Dengan pola origin yang sama ini:

- frontend dan API dapat berada pada domain yang sama, misalnya `https://app.example.com`
- token Bearer dikirim hanya oleh aplikasi frontend ke endpoint API
- CORS tidak perlu disetel di backend

Jangan mengganti `VITE_API_BASE_URL` menjadi URL API publik kecuali frontend dan API memang dipasang pada origin berbeda. Jika berbeda origin, tambahkan origin frontend ke `Cors__AllowedOrigins` di konfigurasi backend.

## Build release

Dari mesin development yang memiliki Node.js 22 atau lebih baru:

```powershell
./scripts/publish-frontend.ps1
```

Hasilnya berada di `.codex-publish/frontend`. Salin seluruh isi folder tersebut ke server pada `/var/www/kh2-management-system/frontend`.

Contoh dari server setelah file terkirim:

```bash
sudo mkdir -p /var/www/kh2-management-system/frontend
sudo rsync -a --delete /tmp/kh2-frontend/ /var/www/kh2-management-system/frontend/
sudo chown -R www-data:www-data /var/www/kh2-management-system/frontend
```

Pastikan sumber `rsync` adalah folder artefak frontend yang spesifik, bukan direktori proyek atau home directory.

## Nginx

Salin `frontend/deploy/nginx.kh2-management-system.conf.example` ke `/etc/nginx/sites-available/kh2-management-system`, kemudian ubah `server_name` menjadi domain produksi. Konfigurasi ini mengasumsikan backend sudah berjalan melalui service systemd pada port `8080` sesuai [panduan backend](deployment-backend-ubuntu.md).

Aktifkan konfigurasi:

```bash
sudo ln -s /etc/nginx/sites-available/kh2-management-system /etc/nginx/sites-enabled/kh2-management-system
sudo nginx -t
sudo systemctl reload nginx
```

## Verifikasi

```bash
curl -I http://127.0.0.1:8080/health
curl -I http://app.example.com/
curl -I http://app.example.com/api/v1/system/info
```

Setelah HTTPS/Cloudflare Tunnel aktif, gunakan URL HTTPS publik untuk verifikasi terakhir.
