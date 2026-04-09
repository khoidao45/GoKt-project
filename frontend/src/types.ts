export interface UserDto {
  id: string
  email: string
  phone?: string | null
  emailVerified: boolean
  status: string
  firstName?: string | null
  lastName?: string | null
  avatarUrl?: string | null
  roles: string[]
  createdAt: string
}

export interface AuthTokensDto {
  accessToken: string
  accessTokenExpiry: string
  refreshToken: string
  refreshTokenExpiry: string
  user: UserDto
}

export interface RegisterResultDto {
  userId: string
  email: string
  verificationExpiresAt: string
  message: string
}

export interface RideRequestDto {
  id: string
  customerId: string
  pickupAddress: string
  pickupLatitude: number
  pickupLongitude: number
  dropoffAddress: string
  dropoffLatitude: number
  dropoffLongitude: number
  vehicleType: string
  status: string
  estimatedFare: number
  estimatedDistanceKm?: number | null
  createdAt: string
  expiresAt: string
}

export interface TripDto {
  id: string
  rideRequestId: string
  driverId: string
  customerId: string
  vehicleId: string
  status: string
  pickupAddress: string
  dropoffAddress: string
  finalFare?: number | null
  actualDistanceKm?: number | null
  actualDurationMinutes?: number | null
  acceptedAt: string
  startedAt?: string | null
  completedAt?: string | null
  cancelledAt?: string | null
  cancellationReason?: string | null
  customerRating?: number | null
  driverRating?: number | null
  driverName?: string | null
  driverAvatarUrl?: string | null
  vehicleMake?: string | null
  vehicleModel?: string | null
  vehicleColor?: string | null
  vehiclePlateNumber?: string | null
  vehicleSeatCount?: number | null
  vehicleImageUrl?: string | null
}

export interface NotificationDto {
  id: string
  title: string
  body: string
  type: string
  isRead: boolean
  createdAt: string
}

export interface ActiveRideDto {
  rideRequest?: RideRequestDto | null
  trip?: TripDto | null
}

export interface PriceEstimateDto {
  vehicleType: string
  estimatedFare: number
  estimatedDistanceKm: number
  currency: string
}

export interface DriverDto {
  id: string
  userId: string
  fullName: string
  avatarUrl?: string | null
  licenseNumber?: string
  licenseExpiry?: string
  status: 'Pending' | 'Active' | 'Suspended'
  rating: number
  totalRides: number
  isOnline: boolean
  isBusy?: boolean
  driverCode?: string | null
  latitude?: number | null
  longitude?: number | null
  vehicles: VehicleDto[]
}

export interface VehicleDto {
  id: string
  driverId: string
  make: string
  model: string
  year: number
  color: string
  plateNumber: string
  seatCount: number
  imageUrl?: string | null
  vehicleType: string
  isActive: boolean
  isVerified: boolean
}

export interface RideOfferPayload {
  rideRequestId: string
  pickupLat: number
  pickupLng: number
  pickupAddress: string
  dropoffLat: number
  dropoffLng: number
  dropoffAddress: string
  vehicleType: string
  estimatedFare: number
  estimatedDistanceKm?: number | null
  expiresAt: string
}

// ─── Admin types ──────────────────────────────────────────────────────────────

export interface AdminStatsDto {
  totalUsers: number
  totalDrivers: number
  pendingDrivers: number
  activeDrivers: number
  totalTrips: number
}

export interface PagedResult<T> {
  items: T[]
  total: number
  page: number
  pageSize: number
}

export interface AdminDriverDto {
  id: string
  userId: string
  fullName: string
  email?: string | null
  phone?: string | null
  avatarUrl?: string | null
  driverCode: string
  licenseNumber: string
  licenseExpiry: string
  status: 'Pending' | 'Active' | 'Suspended'
  rating: number
  totalRides: number
  isOnline: boolean
  vehicles: VehicleDto[]
  createdAt: string
}

export interface PricingRuleDto {
  id: string
  vehicleType: string
  baseFare: number
  perKmRate: number
  perMinuteRate: number
  minimumFare: number
  surgeMultiplier: number
  isActive: boolean
}
