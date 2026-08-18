import type { Dashboard, LoginResponse, SantriList } from './types'

const baseUrl = (import.meta.env.VITE_API_BASE_URL || '/api/v1').replace(/\/$/, '')
const tokenKey = 'kh2.access-token'

export class ApiError extends Error {
  constructor(message: string, public readonly status: number) {
    super(message)
  }
}

export function getToken() {
  return localStorage.getItem(tokenKey)
}

export function clearToken() {
  localStorage.removeItem(tokenKey)
}

async function request<T>(path: string, init: RequestInit = {}): Promise<T> {
  const token = getToken()
  const response = await fetch(`${baseUrl}${path}`, {
    ...init,
    headers: {
      Accept: 'application/json',
      ...(init.body ? { 'Content-Type': 'application/json' } : {}),
      ...(token ? { Authorization: `Bearer ${token}` } : {}),
      ...init.headers,
    },
  })

  if (!response.ok) {
    const error = await response.json().catch(() => null) as { detail?: string; title?: string } | null
    throw new ApiError(error?.detail || error?.title || 'Permintaan ke server gagal.', response.status)
  }

  return response.status === 204 ? undefined as T : response.json() as Promise<T>
}

export async function login(identity: string, password: string) {
  const result = await request<LoginResponse>('/auth/login', {
    method: 'POST',
    body: JSON.stringify({ identity, password }),
  })
  localStorage.setItem(tokenKey, result.accessToken)
  return result
}

export const api = {
  dashboard: () => request<Dashboard>('/dashboard/santri/me'),
  santri: (search = '') => request<SantriList>(`/santri?perPage=10&search=${encodeURIComponent(search)}`),
  logout: () => request<void>('/auth/logout', { method: 'POST' }),
}
