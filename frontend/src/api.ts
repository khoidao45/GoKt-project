import type {
  ActiveRideDto,
  AdminDriverDto,
  AdminStatsDto,
  AuthTokensDto,
  DriverDto,
  NotificationDto,
  PagedResult,
  PriceEstimateDto,
  PricingRuleDto,
  RegisterResultDto,
  RideRequestDto,
  TripDto,
  UserDto,
  VehicleDto,
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
      const text = await res.text()
      if (text) {
        try {
          const data = JSON.parse(text)
          msg = data?.message || data?.title || text
        } catch {
          msg = text
        }
      }
    } catch { /* ignore */ }
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
    request<RegisterResultDto>(
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

  verifyEmailToken: (email: string, token: string) =>
    request<{ message: string }>(
      '/auth/verify-email-token',
      { method: 'POST', body: JSON.stringify({ email, token }) },
      false,
    ),

  resendVerification: (email: string) =>
    request<{ message: string }>(
      '/auth/resend-verification',
      { method: 'POST', body: JSON.stringify({ email }) },
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

// VehicleType enum must match backend: ElectricBike=1, Seat4=2, Seat7=3, Seat9=4
const VEHICLE_TYPE_MAP: Record<string, number> = {
  ElectricBike: 1,
  Seat4: 2,
  Seat7: 3,
  Seat9: 4,
}
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

// ─── Drivers ─────────────────────────────────────────────────────────────────

export const driversApi = {
  register: (licenseNumber: string, licenseExpiry: string) =>
    request<DriverDto>('/drivers/register', {
      method: 'POST',
      body: JSON.stringify({ licenseNumber, licenseExpiry }),
    }),

  addVehicle: (data: {
    make: string; model: string; year: number; color: string
    plateNumber: string; seatCount: number; imageUrl?: string; vehicleType: string
  }) => request<VehicleDto>('/drivers/vehicles', {
    method: 'POST',
    body: JSON.stringify({ ...data, vehicleType: toVehicleTypeInt(data.vehicleType) }),
  }),

  me: () => request<DriverDto | null>('/drivers/me').catch(e => {
    if (e instanceof Error && e.message.includes('404')) return null
    throw e
  }),

  toggleOnline: (isOnline: boolean) =>
    request<void>('/drivers/online', { method: 'PUT', body: JSON.stringify({ isOnline }) }),

  updateLocation: (latitude: number, longitude: number) =>
    request<void>('/drivers/location', { method: 'PUT', body: JSON.stringify({ latitude, longitude }) }),

  trips: (page = 1, pageSize = 20) =>
    request<TripDto[]>(`/drivers/trips?page=${page}&pageSize=${pageSize}`),
}

// ─── Trips (driver actions) ───────────────────────────────────────────────────

export const driverTripsApi = {
  updateStatus: (id: string, status: string) =>
    request<TripDto>(`/trips/${id}/status`, { method: 'PUT', body: JSON.stringify({ status }) }),

  complete: (id: string, actualDistanceKm: number) =>
    request<TripDto>(`/trips/${id}/complete`, { method: 'POST', body: JSON.stringify({ actualDistanceKm }) }),

  rate: (id: string, rating: number, comment?: string) =>
    request<void>(`/trips/${id}/rate`, { method: 'POST', body: JSON.stringify({ rating, comment }) }),
}

// ─── Admin ───────────────────────────────────────────────────────────────────

export const adminApi = {
  stats: () => request<AdminStatsDto>('/admin/stats'),

  users: (page = 1, pageSize = 20) =>
    request<PagedResult<UserDto>>(`/admin/users?page=${page}&pageSize=${pageSize}`),

  setUserStatus: (id: string, status: 'Active' | 'Suspended') =>
    request<UserDto>(`/admin/users/${id}/status`, {
      method: 'PUT',
      body: JSON.stringify({ status }),
    }),

  drivers: (page = 1, pageSize = 20, status?: string) => {
    const q = new URLSearchParams({ page: String(page), pageSize: String(pageSize) })
    if (status) q.set('status', status)
    return request<PagedResult<AdminDriverDto>>(`/admin/drivers?${q}`)
  },

  approveDriver: (id: string) =>
    request<AdminDriverDto>(`/admin/drivers/${id}/approve`, { method: 'PUT' }),

  rejectDriver: (id: string) =>
    request<void>(`/admin/drivers/${id}/reject`, { method: 'PUT' }),

  suspendDriver: (id: string) =>
    request<void>(`/admin/drivers/${id}/suspend`, { method: 'PUT' }),

  trips: (page = 1, pageSize = 20) =>
    request<PagedResult<TripDto>>(`/admin/trips?page=${page}&pageSize=${pageSize}`),

  pricing: () => request<PricingRuleDto[]>('/admin/pricing'),

  updatePricing: (id: string, data: {
    baseFare: number
    perKmRate: number
    perMinuteRate: number
    minimumFare: number
    surgeMultiplier: number
  }) => request<PricingRuleDto>(`/admin/pricing/${id}`, {
    method: 'PUT',
    body: JSON.stringify(data),
  }),
}

// ─── Notifications ────────────────────────────────────────────────────────────

export const notificationsApi = {
  list: (page = 1, pageSize = 20) =>
    request<NotificationDto[]>(`/notifications?page=${page}&pageSize=${pageSize}`),
  markRead: (ids: string[]) =>
    request<void>('/notifications/read', { method: 'PUT', body: JSON.stringify({ notificationIds: ids }) }),
}
