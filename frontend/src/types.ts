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
