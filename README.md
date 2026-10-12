# Backend KH2 Management System

Backend ini disiapkan dengan struktur ASP.NET yang rapi, terpisah per layer, dan siap tumbuh untuk jangka panjang.

## Struktur

```text
.
|-- docs/
|-- src/
|   |-- KH2.ManagementSystem.Api/
|   |-- KH2.ManagementSystem.Application/
|   |-- KH2.ManagementSystem.Domain/
|   |-- KH2.ManagementSystem.Infrastructure/
|   `-- KH2.ManagementSystem.Shared/
|-- tests/
|-- Directory.Build.props
|-- global.json
`-- KH2.ManagementSystem.slnx
```

## Prinsip

- `Api` hanya menangani HTTP dan composition root.
- `Application` menyimpan use case per fitur.
- `Domain` menyimpan entity dan aturan inti.
- `Infrastructure` menyimpan implementasi teknis.
- `Shared` menyimpan primitive lintas layer yang aman dipakai bersama.

## Menjalankan

```powershell
dotnet restore
dotnet build KH2.ManagementSystem.slnx
dotnet run --project src/KH2.ManagementSystem.Api
```

## Deployment

Panduan deployment backend Ubuntu 24.04 + `systemd` + Nginx + PostgreSQL ada di [docs/deployment-backend-ubuntu.md](/d:/project/Backend_KH2-Management-System/docs/deployment-backend-ubuntu.md).

Frontend React/Vite berada di `frontend/`, dibangun menjadi static SPA, dan terhubung ke API melalui `/api/v1`. Panduan build dan deploy Nginx tersedia di [docs/deployment-frontend-ubuntu.md](docs/deployment-frontend-ubuntu.md).

## Endpoint awal

- `GET /health`
- `GET /api/v1/system/info`

Dokumentasi Face Recognition Attendance tersedia di [docs/face-recognition-attendance.md](docs/face-recognition-attendance.md).
