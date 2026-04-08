import type {
  ActiveRideDto,
  AuthTokensDto,
  NotificationDto,
  PriceEstimateDto,
  RideRequestDto,
  TripDto,
  UserDto,
} from './types'

const API_BASE = import.meta.env.VITE_API_BASE_URL ?? 'http://localhost:8080/api/v1'

export const getToken = () => localStorage.getItem('gokt_token') ?? ''
export const setToken = (t: string) =>
  t ? localStorage.setItem('gokt_token', t) : localStorage.removeItem('gokt_token')

async function request<T>(path: string, init?: RequestInit, withAuth = true): Promise<T> {
  const headers = new Headers(init?.headers)
  if (!headers.has('Content-Type')) headers.set('Content-Type', 'application/json')
  if (withAuth) {
    const token = getToken()
    if (token) headers.set('Authorization', `Bearer ${token}`)
  }

  const res = await fetch(`${API_BASE}${path}`, { ...init, headers, credentials: 'include' })

  if (!res.ok) {
    let msg = `Lỗi ${res.status}`
    try {
      const data = await res.json()
      msg = data?.message || data?.title || msg
    } catch {
      const text = await res.text()
      if (text) msg = text
    }
    throw new Error(msg)
  }

  if (res.status === 204) return undefined as T
  return res.json()
}

// ─── Auth ────────────────────────────────────────────────────────────────────

export const auth = {
  login: (email: string, password: string) =>
    request<AuthTokensDto>(
      '/auth/login',
      { method: 'POST', body: JSON.stringify({ email, password }) },
      false,
    ),

  register: (data: {
    email: string
    password: string
    firstName: string
    lastName: string
    phone?: string
  }) =>
    request<AuthTokensDto>(
      '/auth/register',
      { method: 'POST', body: JSON.stringify(data) },
      false,
    ),

  logout: () => request<void>('/auth/logout', { method: 'POST' }),

  forgotPassword: (email: string) =>
    request<{ message: string }>(
      '/auth/forgot-password',
      { method: 'POST', body: JSON.stringify({ email }) },
      false,
    ),

  resetPassword: (token: string, newPassword: string) =>
    request<{ message: string }>(
      '/auth/reset-password',
      { method: 'POST', body: JSON.stringify({ token, newPassword }) },
      false,
    ),

  verifyEmail: (userId: string, token: string) =>
    request<{ message: string }>(
      `/auth/verify-email?userId=${encodeURIComponent(userId)}&token=${encodeURIComponent(token)}`,
      undefined,
      false,
    ),

  changePassword: (currentPassword: string, newPassword: string) =>
    request<{ message: string }>('/auth/change-password', {
      method: 'POST',
      body: JSON.stringify({ currentPassword, newPassword }),
    }),

  google: (idToken: string) =>
    request<AuthTokensDto>(
      '/auth/google',
      { method: 'POST', body: JSON.stringify({ idToken }) },
      false,
    ),
}

// ─── Users ───────────────────────────────────────────────────────────────────

export const users = {
  me: () => request<UserDto>('/users/me'),
  updateProfile: (data: { firstName?: string; lastName?: string; phone?: string }) =>
    request<UserDto>('/users/me/profile', { method: 'PUT', body: JSON.stringify(data) }),
}

// VehicleType enum must match backend: Economy=1, Comfort=2, Premium=3
const VEHICLE_TYPE_MAP: Record<string, number> = { Economy: 1, Comfort: 2, Premium: 3 }
const toVehicleTypeInt = (v: string) => VEHICLE_TYPE_MAP[v] ?? 1

// ─── Rides ───────────────────────────────────────────────────────────────────

export const rides = {
  estimate: (p: {
    pickupLat: number
    pickupLng: number
    dropoffLat: number
    dropoffLng: number
    vehicleType: string
  }) => {
    const q = new URLSearchParams(
      Object.entries(p).map(([k, v]) => [k, String(v)]),
    ).toString()
    return request<PriceEstimateDto>(`/rides/estimate?${q}`)
  },

  request: (data: {
    pickupAddress: string
    pickupLatitude: number
    pickupLongitude: number
    dropoffAddress: string
    dropoffLatitude: number
    dropoffLongitude: number
    vehicleType: string
  }) => request<RideRequestDto>('/rides/request', {
    method: 'POST',
    body: JSON.stringify({ ...data, vehicleType: toVehicleTypeInt(data.vehicleType) }),
  }),

  active: () => request<ActiveRideDto>('/rides/active'),

  cancel: (id: string, reason?: string) =>
    request<void>(`/rides/${id}/cancel`, {
      method: 'POST',
      body: JSON.stringify({ reason: reason ?? '' }),
    }),
}

// ─── Trips ───────────────────────────────────────────────────────────────────

export const tripsApi = {
  history: (page = 1, pageSize = 10) =>
    request<TripDto[]>(`/trips/history?page=${page}&pageSize=${pageSize}`),
}

// ─── Notifications ────────────────────────────────────────────────────────────

export const notificationsApi = {
  list: (page = 1, pageSize = 20) =>
    request<NotificationDto[]>(`/notifications?page=${page}&pageSize=${pageSize}`),
  markRead: (ids: string[]) =>
    request<void>('/notifications/read', { method: 'PUT', body: JSON.stringify({ ids }) }),
}
