import { type FormEvent, type ReactNode, useEffect, useState } from 'react'
import { ApiError, api, clearToken, getToken, login } from './api'
import type { AuthUser, Dashboard, Santri } from './types'

type Page = 'home' | 'santri'

const roleLabel: Record<string, string> = {
  Admin: 'Administrator',
  DewanGuru: 'Dewan Guru',
  Pengurus: 'Pengurus',
  Santri: 'Santri',
  WaliSantri: 'Wali Santri',
}

export function App() {
  const [user, setUser] = useState<AuthUser | null>(null)
  const [page, setPage] = useState<Page>('home')
  const [isCheckingSession, setIsCheckingSession] = useState(Boolean(getToken()))

  useEffect(() => {
    if (!getToken()) return
    // The API's /auth/me response has the same user properties needed here.
    fetch(`${import.meta.env.VITE_API_BASE_URL || '/api/v1'}/auth/me`, {
      headers: { Authorization: `Bearer ${getToken()}` },
    })
      .then(async response => {
        if (!response.ok) throw new Error()
        const me = await response.json() as { username: string; fullName: string; email: string | null; role: string; emailConfirmed: boolean; mustChangePassword: boolean; isActive: boolean }
        setUser(me)
      })
      .catch(clearToken)
      .finally(() => setIsCheckingSession(false))
  }, [])

  if (isCheckingSession) return <main className="centered"><span className="spinner" />Memeriksa sesi…</main>
  if (!user) return <Login onAuthenticated={setUser} />

  return (
    <main className="shell">
      <header className="topbar">
        <a className="brand" href="#home" onClick={() => setPage('home')}>
          <span className="brand-mark">KH²</span>
          <span>Management System</span>
        </a>
        <div className="user-menu">
          <div><strong>{user.fullName}</strong><small>{roleLabel[user.role] ?? user.role}</small></div>
          <button className="button ghost" onClick={async () => { try { await api.logout() } finally { clearToken(); setUser(null) } }}>Keluar</button>
        </div>
      </header>
      <div className="workspace">
        <nav className="sidebar" aria-label="Menu utama">
          <button className={page === 'home' ? 'active' : ''} onClick={() => setPage('home')}>Ringkasan</button>
          <button className={page === 'santri' ? 'active' : ''} onClick={() => setPage('santri')}>Data Santri</button>
        </nav>
        <section className="content">
          {page === 'home' ? <Home user={user} /> : <SantriDirectory />}
        </section>
      </div>
    </main>
  )
}

function Login({ onAuthenticated }: { onAuthenticated: (user: AuthUser) => void }) {
  const [identity, setIdentity] = useState('')
  const [password, setPassword] = useState('')
  const [error, setError] = useState('')
  const [loading, setLoading] = useState(false)

  async function submit(event: FormEvent) {
    event.preventDefault()
    setError('')
    setLoading(true)
    try {
      const response = await login(identity, password)
      const { accessToken: _accessToken, tokenType: _tokenType, expiresInSeconds: _expiresInSeconds, ...user } = response
      onAuthenticated(user)
    } catch (reason) {
      setError(reason instanceof Error ? reason.message : 'Login gagal. Coba lagi.')
    } finally {
      setLoading(false)
    }
  }

  return <main className="login-page">
    <section className="login-panel">
      <div className="brand-lockup"><span className="brand-mark">KH²</span><span>Management System</span></div>
      <p className="eyebrow">PORTAL TERPADU</p>
      <h1>Selamat datang.</h1>
      <p className="muted">Masuk untuk mengakses data akademik, kehadiran, dan aktivitas santri.</p>
      <form onSubmit={submit}>
        <label>Username atau NIS<input value={identity} onChange={e => setIdentity(e.target.value)} autoComplete="username" required /></label>
        <label>Kata sandi<input type="password" value={password} onChange={e => setPassword(e.target.value)} autoComplete="current-password" required /></label>
        {error && <p className="error" role="alert">{error}</p>}
        <button className="button primary full" disabled={loading}>{loading ? 'Memproses…' : 'Masuk ke portal'}</button>
      </form>
    </section>
    <aside className="login-aside"><div><p className="eyebrow">KH2 MANAGEMENT SYSTEM</p><h2>Satu portal untuk pembinaan yang lebih terarah.</h2><p>Informasi penting tersaji ringkas, aman, dan selalu terhubung dengan sistem KH2.</p></div></aside>
  </main>
}

function Home({ user }: { user: AuthUser }) {
  const [dashboard, setDashboard] = useState<Dashboard | null>(null)
  const [error, setError] = useState('')

  useEffect(() => {
    api.dashboard().then(setDashboard).catch(reason => {
      if (reason instanceof ApiError && reason.status === 404) return
      setError(reason instanceof Error ? reason.message : 'Data ringkasan belum dapat dimuat.')
    })
  }, [])

  if (!dashboard) return <section>
    <p className="eyebrow">RINGKASAN</p>
    <h1>Assalamu’alaikum, {user.fullName.split(' ')[0]}.</h1>
    {error ? <p className="error">{error}</p> : <StaffWelcome user={user} />}
  </section>

  const stats = [
    ['Kehadiran', `${dashboard.highlight.attendancePercentage}%`, 'Persentase kehadiran'],
    ['Progres belajar', `${dashboard.highlight.averageProgressPercentage}%`, 'Rata-rata capaian'],
    ['Sisa kafarah', `${dashboard.highlight.remainingKafarah}`, 'Tanggungan aktif'],
    ['Aktivitas', `${dashboard.highlight.recordedLogs}`, 'Catatan tercatat'],
  ]
  return <section>
    <p className="eyebrow">RINGKASAN SANTRI</p>
    <h1>Assalamu’alaikum, {dashboard.profile.fullName.split(' ')[0]}.</h1>
    <p className="muted">{dashboard.profile.kampus} · {dashboard.profile.jurusan} · Kelas {dashboard.profile.kelas}</p>
    <div className="stats">{stats.map(([label, value, note]) => <article className="stat" key={label}><span>{label}</span><strong>{value}</strong><small>{note}</small></article>)}</div>
    <div className="grids">
      <Panel title="Kehadiran terbaru"><ActivityTable items={dashboard.attendance.recent} /></Panel>
      <Panel title="Progres keilmuan"><ProgressList items={dashboard.progress.recent} /></Panel>
      <Panel title="Aktivitas keluar-masuk"><LogList items={dashboard.logs.recent} /></Panel>
    </div>
  </section>
}

function StaffWelcome({ user }: { user: AuthUser }) {
  return <article className="welcome-card"><h2>Portal siap digunakan</h2><p>Anda masuk sebagai {roleLabel[user.role] ?? user.role}. Gunakan menu <strong>Data Santri</strong> untuk mencari data sesuai kewenangan akun.</p></article>
}

function SantriDirectory() {
  const [search, setSearch] = useState('')
  const [items, setItems] = useState<Santri[]>([])
  const [total, setTotal] = useState<number | null>(null)
  const [error, setError] = useState('')
  const [loading, setLoading] = useState(true)

  useEffect(() => {
    const timeout = window.setTimeout(() => {
      setLoading(true)
      api.santri(search).then(result => { setItems(result.items); setTotal(result.totalCount); setError('') }).catch(reason => setError(reason instanceof Error ? reason.message : 'Data santri tidak dapat dimuat.')).finally(() => setLoading(false))
    }, 250)
    return () => window.clearTimeout(timeout)
  }, [search])

  return <section>
    <p className="eyebrow">DIREKTORI</p><h1>Data santri</h1>
    <div className="directory-tools"><input aria-label="Cari santri" placeholder="Cari nama, NIS, tim, atau kelas…" value={search} onChange={e => setSearch(e.target.value)} /><span>{total === null ? 'Memuat…' : `${total} data`}</span></div>
    {error ? <p className="error">{error}</p> : <div className="table-wrap"><table><thead><tr><th>Nama</th><th>NIS</th><th>Kelas</th><th>Tim</th><th>Kampus</th></tr></thead><tbody>{loading ? <tr><td colSpan={5}>Memuat data…</td></tr> : items.length ? items.map(item => <tr key={item.id}><td><strong>{item.nama}</strong><small>{item.jurusan}</small></td><td>{item.nis}</td><td>{item.kelas}</td><td><span className="pill">{item.tim}</span></td><td>{item.kampus}</td></tr>) : <tr><td colSpan={5}>Tidak ada data yang sesuai.</td></tr>}</tbody></table></div>}
  </section>
}

function Panel({ title, children }: { title: string; children: ReactNode }) { return <article className="panel"><h2>{title}</h2>{children}</article> }
function ActivityTable({ items }: { items: Dashboard['attendance']['recent'] }) { return <ul className="compact-list">{items.length ? items.map(item => <li key={item.id}><div><strong>{item.kegiatanKategori}</strong><span>{formatDate(item.tanggal)} · {item.waktu}</span></div><span className={`status ${item.status}`}>{item.status}</span></li>) : <Empty />}</ul> }
function ProgressList({ items }: { items: Dashboard['progress']['recent'] }) { return <ul className="compact-list">{items.length ? items.map(item => <li key={item.id}><div><strong>{item.judul}</strong><span>{item.capaian}/{item.target} {item.satuan ?? ''}</span></div><span className="progress-value">{item.persentase}%</span></li>) : <Empty />}</ul> }
function LogList({ items }: { items: Dashboard['logs']['recent'] }) { return <ul className="compact-list">{items.length ? items.map(item => <li key={item.id}><div><strong>{item.jenis}</strong><span>{formatDate(item.tanggalPengajuan)} {item.rentang ? `· ${item.rentang}` : ''}</span></div><span className="status neutral">{item.status}</span></li>) : <Empty />}</ul> }
function Empty() { return <li className="muted">Belum ada data tercatat.</li> }
function formatDate(value: string) { return new Intl.DateTimeFormat('id-ID', { day: 'numeric', month: 'short', year: 'numeric' }).format(new Date(`${value}T00:00:00`)) }
