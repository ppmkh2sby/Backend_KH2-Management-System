export type LoginResponse = {
  accessToken: string
  tokenType: string
  expiresInSeconds: number
  username: string
  fullName: string
  email: string | null
  role: string
  emailConfirmed: boolean
  mustChangePassword: boolean
  isActive: boolean
}

export type AuthUser = Omit<LoginResponse, 'accessToken' | 'tokenType' | 'expiresInSeconds'>

export type Dashboard = {
  generatedAtUtc: string
  profile: {
    fullName: string
    nis: string
    kampus: string
    jurusan: string
    gender: string
    tim: string
    kelas: string
  }
  highlight: {
    attendancePercentage: number
    remainingKafarah: number
    averageProgressPercentage: number
    recordedLogs: number
  }
  attendance: {
    total: number
    hadir: number
    izin: number
    sakit: number
    alpha: number
    persentase: number
    recent: Activity[]
  }
  progress: {
    total: number
    completed: number
    inProgress: number
    average: number
    recent: ProgressItem[]
  }
  logs: { total: number; tercatat: number; recent: LogItem[] }
}

export type Activity = { id: string; tanggal: string; nama: string; kegiatanKategori: string; waktu: string; status: string; catatan: string | null }
export type ProgressItem = { id: string; judul: string; target: number; capaian: number; satuan: string | null; persentase: number; catatan: string | null }
export type LogItem = { id: string; tanggalPengajuan: string; jenis: string; rentang: string | null; status: string; catatan: string | null }
export type Santri = { id: string; nama: string; nis: string; kampus: string; jurusan: string; gender: string; tim: string; kelas: string }
export type SantriList = { items: Santri[]; page: number; perPage: number; totalCount: number }
