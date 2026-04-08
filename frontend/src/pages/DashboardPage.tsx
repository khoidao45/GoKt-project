import { useState, useEffect, useCallback } from 'react'
import { useNavigate } from 'react-router-dom'
import { auth, rides, tripsApi, notificationsApi, users } from '../api'
import MapPicker from '../components/MapPicker'
import type { LatLng } from '../components/MapPicker'
import type {
  UserDto, ActiveRideDto, TripDto, NotificationDto, PriceEstimateDto,
} from '../types'

// ─── Geocoding ────────────────────────────────────────────────────────────

async function reverseGeocode(lat: number, lng: number): Promise<string> {
  try {
    const res = await fetch(
      `https://nominatim.openstreetmap.org/reverse?lat=${lat}&lon=${lng}&format=json`,
      { headers: { 'Accept-Language': 'vi' } },
    )
    const data = await res.json()
    return data.display_name ?? `${lat.toFixed(5)}, ${lng.toFixed(5)}`
  } catch {
    return `${lat.toFixed(5)}, ${lng.toFixed(5)}`
  }
}

// ─── Helpers ──────────────────────────────────────────────────────────────

function statusBadge(status: string) {
  const map: Record<string, string> = {
    Pending: 'badge-warning', Searching: 'badge-warning',
    Accepted: 'badge-info',   InProgress: 'badge-info',
    Completed: 'badge-success', Cancelled: 'badge-danger',
    Failed: 'badge-danger',
  }
  const cls = map[status] ?? 'badge-neutral'
  return <span className={`badge ${cls}`}>{statusLabel(status)}</span>
}

function statusLabel(s: string) {
  const map: Record<string, string> = {
    Pending: 'Chờ tài xế', Searching: 'Đang tìm',
    Accepted: 'Đã nhận',   InProgress: 'Đang đi',
    Completed: 'Hoàn thành', Cancelled: 'Đã huỷ',
    Failed: 'Thất bại',
  }
  return map[s] ?? s
}

function fmtTime(iso: string) {
  return new Date(iso).toLocaleString('vi-VN', {
    day: '2-digit', month: '2-digit', year: 'numeric',
    hour: '2-digit', minute: '2-digit',
  })
}

function fmtCurrency(n?: number | null) {
  if (n == null) return '—'
  return new Intl.NumberFormat('vi-VN', { style: 'currency', currency: 'VND' }).format(n * 23000)
}

type Tab = 'overview' | 'request' | 'history' | 'notifications' | 'profile'

interface Props { user: UserDto; onLogout: () => void }

// ─── Main Component ───────────────────────────────────────────────────────

export default function DashboardPage({ user: initialUser, onLogout }: Props) {
  const nav = useNavigate()
  const [tab, setTab] = useState<Tab>('overview')
  const [user, setUser] = useState(initialUser)

  const [activeRide, setActiveRide] = useState<ActiveRideDto | null>(null)
  const [trips, setTrips] = useState<TripDto[]>([])
  const [notifs, setNotifs] = useState<NotificationDto[]>([])
  const [estimate, setEstimate] = useState<PriceEstimateDto | null>(null)

  const [loading, setLoading] = useState(false)
  const [toast, setToast] = useState<{ msg: string; type: 'success' | 'error' } | null>(null)

  // ride form
  const [pickupPos, setPickupPos] = useState<LatLng | null>(null)
  const [pickupAddr, setPickupAddr] = useState('')
  const [dropoffPos, setDropoffPos] = useState<LatLng | null>(null)
  const [dropoffAddr, setDropoffAddr] = useState('')
  const [mapMode, setMapMode] = useState<'pickup' | 'dropoff'>('pickup')
  const [vehicleType, setVehicleType] = useState('Economy')
  const [gpsLoading, setGpsLoading] = useState(false)

  // profile form
  const [profileForm, setProfileForm] = useState({
    firstName: user.firstName ?? '',
    lastName: user.lastName ?? '',
    phone: user.phone ?? '',
  })
  const [pwdForm, setPwdForm] = useState({ current: '', next: '', confirm: '' })
  const [profileBusy, setProfileBusy] = useState(false)

  const showToast = (msg: string, type: 'success' | 'error' = 'success') => {
    setToast({ msg, type })
    setTimeout(() => setToast(null), 3500)
  }

  const run = useCallback(async (work: () => Promise<void>) => {
    setLoading(true)
    try { await work() } catch (e) {
      showToast(e instanceof Error ? e.message : 'Có lỗi xảy ra', 'error')
    } finally { setLoading(false) }
  }, [])

  const loadOverview = useCallback(() => run(async () => {
    const [active, history, ns] = await Promise.all([
      rides.active(),
      tripsApi.history(),
      notificationsApi.list(),
    ])
    setActiveRide(active)
    setTrips(history)
    setNotifs(ns)
  }), [run])

  useEffect(() => { loadOverview() }, [loadOverview])

  const handleLogout = () => run(async () => {
    await auth.logout()
    onLogout()
    nav('/login', { replace: true })
  })

  const useMyLocation = () => {
    if (!navigator.geolocation) { showToast('Trình duyệt không hỗ trợ GPS', 'error'); return }
    setGpsLoading(true)
    navigator.geolocation.getCurrentPosition(
      async (pos) => {
        const { latitude: lat, longitude: lng } = pos.coords
        const addr = await reverseGeocode(lat, lng)
        setPickupPos({ lat, lng })
        setPickupAddr(addr)
        setMapMode('dropoff')
        setGpsLoading(false)
        showToast('📍 Đã lấy vị trí hiện tại')
      },
      () => {
        setGpsLoading(false)
        showToast('Không lấy được vị trí GPS', 'error')
      },
      { enableHighAccuracy: true, timeout: 8000 },
    )
  }

  const doEstimate = () => run(async () => {
    if (!pickupPos || !dropoffPos) { showToast('Vui lòng chọn điểm đón và điểm đến', 'error'); return }
    const res = await rides.estimate({
      pickupLat: pickupPos.lat, pickupLng: pickupPos.lng,
      dropoffLat: dropoffPos.lat, dropoffLng: dropoffPos.lng,
      vehicleType,
    })
    setEstimate(res)
    showToast('Đã ước tính giá chuyến đi')
  })

  const doRequestRide = () => run(async () => {
    if (!pickupPos || !dropoffPos) { showToast('Vui lòng chọn điểm đón và điểm đến', 'error'); return }
    const ride = await rides.request({
      pickupAddress: pickupAddr, pickupLatitude: pickupPos.lat, pickupLongitude: pickupPos.lng,
      dropoffAddress: dropoffAddr, dropoffLatitude: dropoffPos.lat, dropoffLongitude: dropoffPos.lng,
      vehicleType,
    })
    setActiveRide({ rideRequest: ride, trip: null })
    setTab('overview')
    showToast('🚗 Đang tìm tài xế cho bạn!')
  })

  const doCancelRide = (id: string) => run(async () => {
    await rides.cancel(id, 'Huỷ bởi khách hàng')
    setActiveRide(null)
    showToast('Đã huỷ chuyến đi', 'error')
  })

  const doMarkRead = (ids: string[]) => run(async () => {
    await notificationsApi.markRead(ids)
    setNotifs((ns) => ns.map((n) => ids.includes(n.id) ? { ...n, isRead: true } : n))
  })

  const doUpdateProfile = () => {
    setProfileBusy(true)
    run(async () => {
      const updated = await users.updateProfile(profileForm)
      setUser(updated)
      showToast('Cập nhật hồ sơ thành công')
    }).finally(() => setProfileBusy(false))
  }

  const doChangePassword = () => {
    if (pwdForm.next !== pwdForm.confirm) { showToast('Mật khẩu xác nhận không khớp', 'error'); return }
    setProfileBusy(true)
    run(async () => {
      await auth.changePassword(pwdForm.current, pwdForm.next)
      setPwdForm({ current: '', next: '', confirm: '' })
      showToast('Đổi mật khẩu thành công. Vui lòng đăng nhập lại.')
      setTimeout(() => { onLogout(); nav('/login') }, 2000)
    }).finally(() => setProfileBusy(false))
  }

  const fullName = [user.firstName, user.lastName].filter(Boolean).join(' ') || user.email
  const initials = [user.firstName?.[0], user.lastName?.[0]].filter(Boolean).join('').toUpperCase() || user.email[0].toUpperCase()
  const unreadCount = notifs.filter((n) => !n.isRead).length

  const vehicles = [
    { type: 'Economy', icon: '🛵', name: 'Economy', sub: 'Tiết kiệm' },
    { type: 'Comfort',  icon: '🚗', name: 'Comfort',  sub: 'Thoải mái' },
    { type: 'Premium',  icon: '🚙', name: 'Premium',  sub: 'Cao cấp' },
  ]

  // ─── Sidebar ─────────────────────────────────────────────────────────────

  const navItems: { id: Tab; icon: string; label: string; badge?: number }[] = [
    { id: 'overview',       icon: '🏠', label: 'Tổng quan' },
    { id: 'request',        icon: '🚖', label: 'Đặt xe' },
    { id: 'history',        icon: '📋', label: 'Lịch sử' },
    { id: 'notifications',  icon: '🔔', label: 'Thông báo', badge: unreadCount || undefined },
    { id: 'profile',        icon: '👤', label: 'Hồ sơ' },
  ]

  // ─── Render ───────────────────────────────────────────────────────────────

  return (
    <div className="dash-shell">
      {/* Sidebar */}
      <aside className="sidebar">
        <div className="sidebar-logo">
          <div className="sidebar-logo-icon">G</div>
          <span className="sidebar-logo-name">Gokt</span>
        </div>

        <nav className="sidebar-nav">
          <p className="sidebar-section-label">Menu</p>
          {navItems.map((item) => (
            <button
              key={item.id}
              className={`nav-item${tab === item.id ? ' active' : ''}`}
              onClick={() => setTab(item.id)}
            >
              <span className="nav-icon">{item.icon}</span>
              {item.label}
              {item.badge ? <span className="nav-badge">{item.badge}</span> : null}
            </button>
          ))}
        </nav>

        <div className="sidebar-footer">
          <div className="sidebar-user">
            <div className="sidebar-avatar">{initials}</div>
            <div className="sidebar-user-info">
              <div className="sidebar-user-name">{fullName}</div>
              <div className="sidebar-user-email">{user.email}</div>
            </div>
          </div>
          <button className="nav-item" onClick={handleLogout} disabled={loading}>
            <span className="nav-icon">🚪</span>
            Đăng xuất
          </button>
        </div>
      </aside>

      {/* Main */}
      <main className="dash-main">
        <div className="dash-topbar">
          <div>
            <div className="dash-topbar-title">
              {navItems.find((n) => n.id === tab)?.icon}{' '}
              {navItems.find((n) => n.id === tab)?.label}
            </div>
          </div>
          <div className="flex items-center gap-3">
            {!user.emailVerified && (
              <div className="alert alert-warning" style={{ margin: 0, padding: '6px 12px', fontSize: 12 }}>
                ⚠️ Email chưa xác thực
              </div>
            )}
            <button
              className="btn btn-ghost btn-sm"
              onClick={loadOverview}
              disabled={loading}
            >
              {loading ? <span className="spinner spinner-dark" /> : '↻'} Làm mới
            </button>
          </div>
        </div>

        <div className="dash-content fade-in">

          {/* ── Overview Tab ──────────────────────────────────────────── */}
          {tab === 'overview' && (
            <>
              <div className="stats-grid">
                <div className="stat-card">
                  <div className="stat-icon">🚗</div>
                  <div className="stat-label">Tổng chuyến</div>
                  <div className="stat-value">{trips.length}</div>
                </div>
                <div className="stat-card">
                  <div className="stat-icon">✅</div>
                  <div className="stat-label">Hoàn thành</div>
                  <div className="stat-value">{trips.filter((t) => t.status === 'Completed').length}</div>
                </div>
                <div className="stat-card">
                  <div className="stat-icon">🔔</div>
                  <div className="stat-label">Thông báo mới</div>
                  <div className="stat-value">{unreadCount}</div>
                </div>
                <div className="stat-card">
                  <div className="stat-icon">👤</div>
                  <div className="stat-label">Vai trò</div>
                  <div className="stat-value" style={{ fontSize: 16 }}>
                    {user.roles?.join(', ') || 'RIDER'}
                  </div>
                </div>
              </div>

              {/* Active ride */}
              {activeRide?.rideRequest || activeRide?.trip ? (
                <div style={{ marginBottom: 20 }}>
                  <h3 className="font-bold mb-2" style={{ fontSize: 15 }}>Chuyến đi hiện tại</h3>
                  {activeRide.rideRequest && (
                    <div className="ride-status-card mb-4">
                      <div className="ride-status-badge">
                        {statusLabel(activeRide.rideRequest.status)}
                      </div>
                      <div style={{ fontSize: 20, fontWeight: 700, marginBottom: 4 }}>
                        {fmtCurrency(activeRide.rideRequest.estimatedFare)}
                      </div>
                      <div style={{ fontSize: 13, opacity: .7, marginBottom: 12 }}>
                        {activeRide.rideRequest.vehicleType} •{' '}
                        {activeRide.rideRequest.estimatedDistanceKm?.toFixed(1)} km
                      </div>
                      <div className="ride-route">
                        <div className="route-point">
                          <div className="route-dot pickup" />
                          {activeRide.rideRequest.pickupAddress}
                        </div>
                        <div className="route-point">
                          <div className="route-dot dropoff" />
                          {activeRide.rideRequest.dropoffAddress}
                        </div>
                      </div>
                      {['Pending', 'Searching'].includes(activeRide.rideRequest.status) && (
                        <button
                          className="btn btn-danger btn-sm mt-3"
                          onClick={() => doCancelRide(activeRide.rideRequest!.id)}
                          disabled={loading}
                        >
                          Huỷ chuyến
                        </button>
                      )}
                    </div>
                  )}
                </div>
              ) : (
                <div className="card" style={{ marginBottom: 20 }}>
                  <div className="card-body">
                    <div className="empty-state" style={{ padding: '32px 24px' }}>
                      <div className="empty-icon">🛣️</div>
                      <div className="empty-title">Không có chuyến đi nào đang diễn ra</div>
                      <div className="empty-desc mb-4">Bạn chưa có chuyến đi nào đang hoạt động</div>
                      <button
                        className="btn btn-primary"
                        onClick={() => setTab('request')}
                      >
                        Đặt xe ngay
                      </button>
                    </div>
                  </div>
                </div>
              )}

              {/* Recent trips */}
              <div className="card">
                <div className="card-header">
                  <span className="card-title">Chuyến gần đây</span>
                  <button className="btn btn-ghost btn-sm" onClick={() => setTab('history')}>
                    Xem tất cả →
                  </button>
                </div>
                <div className="card-body">
                  {trips.length === 0 ? (
                    <div className="empty-state" style={{ padding: '24px' }}>
                      <div className="empty-desc">Chưa có chuyến đi nào</div>
                    </div>
                  ) : trips.slice(0, 3).map((t) => (
                    <div key={t.id} className="list-item">
                      <div className="list-item-icon">🚗</div>
                      <div className="list-item-body">
                        <div className="list-item-title">{t.pickupAddress} → {t.dropoffAddress}</div>
                        <div className="list-item-meta">
                          {statusBadge(t.status)}
                          <span className="meta-dot" />
                          <span>{fmtTime(t.acceptedAt)}</span>
                          {t.finalFare != null && (
                            <>
                              <span className="meta-dot" />
                              <span>{fmtCurrency(t.finalFare)}</span>
                            </>
                          )}
                        </div>
                      </div>
                    </div>
                  ))}
                </div>
              </div>
            </>
          )}

          {/* ── Request Tab ───────────────────────────────────────────── */}
          {tab === 'request' && (
            <div style={{ maxWidth: 680 }}>
              {/* Map picker */}
              <div className="card mb-4">
                <div className="card-header">
                  <span className="card-title">🗺️ Chọn địa điểm trên bản đồ</span>
                  <button
                    className="btn btn-secondary btn-sm"
                    onClick={useMyLocation}
                    disabled={gpsLoading}
                  >
                    {gpsLoading ? <span className="spinner spinner-dark" /> : '📍'}
                    {gpsLoading ? 'Đang lấy GPS...' : 'Vị trí hiện tại'}
                  </button>
                </div>
                <div className="card-body">
                  {/* Mode toggle */}
                  <div className="flex gap-2 mb-3">
                    <button
                      className={`btn btn-sm${mapMode === 'pickup' ? ' btn-primary' : ' btn-ghost'}`}
                      onClick={() => setMapMode('pickup')}
                    >
                      🟢 Chọn điểm đón
                    </button>
                    <button
                      className={`btn btn-sm${mapMode === 'dropoff' ? ' btn-primary' : ' btn-ghost'}`}
                      onClick={() => setMapMode('dropoff')}
                    >
                      🔴 Chọn điểm đến
                    </button>
                  </div>

                  <MapPicker
                    pickup={pickupPos}
                    dropoff={dropoffPos}
                    mode={mapMode}
                    onPickupChange={(pos, addr) => {
                      setPickupPos(pos)
                      setPickupAddr(addr)
                      setEstimate(null)
                      setMapMode('dropoff')
                    }}
                    onDropoffChange={(pos, addr) => {
                      setDropoffPos(pos)
                      setDropoffAddr(addr)
                      setEstimate(null)
                    }}
                  />

                  {/* Address display */}
                  <div style={{ display: 'grid', gap: 10, marginTop: 14 }}>
                    <div style={{
                      display: 'flex', alignItems: 'flex-start', gap: 10,
                      padding: '10px 14px', background: pickupPos ? '#f0fdf4' : '#f8fafc',
                      borderRadius: 8, border: `1.5px solid ${pickupPos ? '#86efac' : 'var(--border)'}`,
                      fontSize: 13,
                    }}>
                      <span style={{ fontSize: 16, flexShrink: 0, marginTop: 1 }}>🟢</span>
                      <div style={{ flex: 1, minWidth: 0 }}>
                        <div style={{ fontWeight: 600, color: 'var(--text-2)', marginBottom: 2, fontSize: 11, textTransform: 'uppercase', letterSpacing: '.05em' }}>Điểm đón</div>
                        <div style={{ color: pickupPos ? 'var(--text)' : 'var(--muted)', wordBreak: 'break-word' }}>
                          {pickupAddr || 'Chưa chọn — nhấn nút GPS hoặc click bản đồ'}
                        </div>
                      </div>
                      {pickupPos && (
                        <button className="btn btn-sm btn-ghost" style={{ flexShrink: 0, padding: '0 8px', height: 28, fontSize: 12 }}
                          onClick={() => { setPickupPos(null); setPickupAddr(''); setEstimate(null) }}>✕</button>
                      )}
                    </div>

                    <div style={{
                      display: 'flex', alignItems: 'flex-start', gap: 10,
                      padding: '10px 14px', background: dropoffPos ? '#fef2f2' : '#f8fafc',
                      borderRadius: 8, border: `1.5px solid ${dropoffPos ? '#fca5a5' : 'var(--border)'}`,
                      fontSize: 13,
                    }}>
                      <span style={{ fontSize: 16, flexShrink: 0, marginTop: 1 }}>🔴</span>
                      <div style={{ flex: 1, minWidth: 0 }}>
                        <div style={{ fontWeight: 600, color: 'var(--text-2)', marginBottom: 2, fontSize: 11, textTransform: 'uppercase', letterSpacing: '.05em' }}>Điểm đến</div>
                        <div style={{ color: dropoffPos ? 'var(--text)' : 'var(--muted)', wordBreak: 'break-word' }}>
                          {dropoffAddr || 'Chưa chọn — click vào bản đồ để chọn'}
                        </div>
                      </div>
                      {dropoffPos && (
                        <button className="btn btn-sm btn-ghost" style={{ flexShrink: 0, padding: '0 8px', height: 28, fontSize: 12 }}
                          onClick={() => { setDropoffPos(null); setDropoffAddr(''); setEstimate(null) }}>✕</button>
                      )}
                    </div>
                  </div>
                </div>
              </div>

              {/* Vehicle + actions */}
              <div className="card">
                <div className="card-header">
                  <span className="card-title">Chọn loại xe</span>
                </div>
                <div className="card-body">
                  <div className="vehicle-options">
                    {vehicles.map((v) => (
                      <button
                        key={v.type}
                        className={`vehicle-opt${vehicleType === v.type ? ' selected' : ''}`}
                        onClick={() => { setVehicleType(v.type); setEstimate(null) }}
                      >
                        <div className="vehicle-opt-icon">{v.icon}</div>
                        <div className="vehicle-opt-name">{v.name}</div>
                        <div className="vehicle-opt-price">{v.sub}</div>
                      </button>
                    ))}
                  </div>

                  {estimate && (
                    <div className="estimate-result">
                      <div>
                        <div className="estimate-fare">{fmtCurrency(estimate.estimatedFare)}</div>
                        <div className="estimate-dist">
                          {estimate.estimatedDistanceKm.toFixed(1)} km • {estimate.vehicleType}
                        </div>
                      </div>
                    </div>
                  )}

                  <div className="flex gap-3 mt-4">
                    <button
                      className="btn btn-ghost"
                      onClick={doEstimate}
                      disabled={loading || !pickupPos || !dropoffPos}
                      style={{ flex: 1 }}
                    >
                      {loading ? <span className="spinner spinner-dark" /> : '💰'}
                      Ước tính giá
                    </button>
                    <button
                      className="btn btn-primary"
                      onClick={doRequestRide}
                      disabled={loading || !pickupPos || !dropoffPos}
                      style={{ flex: 1 }}
                    >
                      {loading ? <span className="spinner" /> : '🚖'}
                      Đặt xe ngay
                    </button>
                  </div>

                  {(!pickupPos || !dropoffPos) && (
                    <p className="text-sm text-muted mt-2" style={{ textAlign: 'center' }}>
                      Vui lòng chọn đủ điểm đón và điểm đến trước khi đặt xe
                    </p>
                  )}
                </div>
              </div>
            </div>
          )}

          {/* ── History Tab ───────────────────────────────────────────── */}
          {tab === 'history' && (
            <div className="card">
              <div className="card-header">
                <span className="card-title">Lịch sử chuyến đi</span>
                <span className="badge badge-neutral">{trips.length} chuyến</span>
              </div>
              <div className="card-body">
                {trips.length === 0 ? (
                  <div className="empty-state">
                    <div className="empty-icon">🛣️</div>
                    <div className="empty-title">Chưa có chuyến đi nào</div>
                    <div className="empty-desc mb-4">Đặt chuyến đầu tiên của bạn ngay!</div>
                    <button className="btn btn-primary" onClick={() => setTab('request')}>
                      Đặt xe ngay
                    </button>
                  </div>
                ) : trips.map((t) => (
                  <div key={t.id} className="list-item">
                    <div className="list-item-icon" style={{
                      background: t.status === 'Completed' ? 'var(--success-light)' :
                                  t.status === 'Cancelled' ? 'var(--danger-light)' : 'var(--primary-light)',
                      color: t.status === 'Completed' ? '#047857' :
                             t.status === 'Cancelled' ? '#b91c1c' : 'var(--primary-dark)',
                    }}>
                      {t.status === 'Completed' ? '✅' : t.status === 'Cancelled' ? '❌' : '🚗'}
                    </div>
                    <div className="list-item-body">
                      <div className="list-item-title">
                        {t.pickupAddress} → {t.dropoffAddress}
                      </div>
                      <div className="list-item-meta">
                        {statusBadge(t.status)}
                        <span className="meta-dot" />
                        <span>{fmtTime(t.acceptedAt)}</span>
                        {t.actualDistanceKm != null && (
                          <>
                            <span className="meta-dot" />
                            <span>{t.actualDistanceKm.toFixed(1)} km</span>
                          </>
                        )}
                        {t.finalFare != null && (
                          <>
                            <span className="meta-dot" />
                            <span style={{ fontWeight: 600, color: 'var(--text)' }}>
                              {fmtCurrency(t.finalFare)}
                            </span>
                          </>
                        )}
                      </div>
                      {t.cancellationReason && (
                        <div className="text-xs text-muted mt-1">
                          Lý do huỷ: {t.cancellationReason}
                        </div>
                      )}
                    </div>
                  </div>
                ))}
              </div>
            </div>
          )}

          {/* ── Notifications Tab ─────────────────────────────────────── */}
          {tab === 'notifications' && (
            <div className="card">
              <div className="card-header">
                <span className="card-title">Thông báo</span>
                {unreadCount > 0 && (
                  <button
                    className="btn btn-secondary btn-sm"
                    onClick={() => doMarkRead(notifs.filter((n) => !n.isRead).map((n) => n.id))}
                    disabled={loading}
                  >
                    Đọc tất cả
                  </button>
                )}
              </div>
              <div className="card-body">
                {notifs.length === 0 ? (
                  <div className="empty-state">
                    <div className="empty-icon">🔔</div>
                    <div className="empty-title">Không có thông báo</div>
                    <div className="empty-desc">Bạn đã cập nhật tất cả thông báo</div>
                  </div>
                ) : notifs.map((n) => (
                  <div
                    key={n.id}
                    className={`notif-item${!n.isRead ? ' unread' : ''}`}
                    onClick={() => !n.isRead && doMarkRead([n.id])}
                  >
                    <div className={`notif-dot${n.isRead ? ' read' : ''}`} />
                    <div style={{ flex: 1, minWidth: 0 }}>
                      <div className="notif-title">{n.title}</div>
                      <div className="notif-body">{n.body}</div>
                      <div className="notif-time">{fmtTime(n.createdAt)}</div>
                    </div>
                  </div>
                ))}
              </div>
            </div>
          )}

          {/* ── Profile Tab ───────────────────────────────────────────── */}
          {tab === 'profile' && (
            <div style={{ maxWidth: 560, display: 'flex', flexDirection: 'column', gap: 20 }}>
              {/* Info card */}
              <div className="card">
                <div className="card-header">
                  <span className="card-title">Thông tin cá nhân</span>
                </div>
                <div className="card-body">
                  <div style={{
                    display: 'flex', alignItems: 'center', gap: 16, marginBottom: 24,
                    padding: 16, background: 'var(--bg)', borderRadius: 'var(--radius)',
                  }}>
                    <div style={{
                      width: 56, height: 56, borderRadius: '50%',
                      background: 'var(--primary-grad)', display: 'flex', alignItems: 'center',
                      justifyContent: 'center', color: '#fff', fontSize: 22, fontWeight: 700,
                    }}>
                      {initials}
                    </div>
                    <div>
                      <div style={{ fontWeight: 700, fontSize: 16 }}>{fullName}</div>
                      <div className="text-sm text-muted">{user.email}</div>
                      <div className="flex gap-2 mt-1">
                        {user.roles?.map((r) => (
                          <span key={r} className="badge badge-info">{r}</span>
                        ))}
                        {user.emailVerified
                          ? <span className="badge badge-success">✉️ Đã xác thực</span>
                          : <span className="badge badge-warning">⚠️ Chưa xác thực email</span>
                        }
                      </div>
                    </div>
                  </div>

                  <div className="field-row">
                    <div className="field" style={{ marginBottom: 0 }}>
                      <label>Họ</label>
                      <input
                        value={profileForm.lastName}
                        onChange={(e) => setProfileForm((f) => ({ ...f, lastName: e.target.value }))}
                      />
                    </div>
                    <div className="field" style={{ marginBottom: 0 }}>
                      <label>Tên</label>
                      <input
                        value={profileForm.firstName}
                        onChange={(e) => setProfileForm((f) => ({ ...f, firstName: e.target.value }))}
                      />
                    </div>
                  </div>

                  <div className="field mt-4">
                    <label>Số điện thoại</label>
                    <input
                      type="tel"
                      value={profileForm.phone}
                      onChange={(e) => setProfileForm((f) => ({ ...f, phone: e.target.value }))}
                      placeholder="+84 901 234 567"
                    />
                  </div>

                  <div className="field">
                    <label>Email</label>
                    <input type="email" value={user.email} disabled style={{ opacity: .6 }} />
                  </div>

                  <button
                    className="btn btn-primary"
                    onClick={doUpdateProfile}
                    disabled={profileBusy || loading}
                  >
                    {profileBusy ? <span className="spinner" /> : null}
                    Lưu thay đổi
                  </button>
                </div>
              </div>

              {/* Change password card */}
              <div className="card">
                <div className="card-header">
                  <span className="card-title">🔐 Đổi mật khẩu</span>
                </div>
                <div className="card-body">
                  <div className="alert alert-warning" style={{ marginBottom: 16 }}>
                    <span className="alert-icon">⚠️</span>
                    Đổi mật khẩu sẽ đăng xuất tất cả thiết bị khác.
                  </div>
                  <div className="field">
                    <label>Mật khẩu hiện tại</label>
                    <input
                      type="password"
                      value={pwdForm.current}
                      onChange={(e) => setPwdForm((f) => ({ ...f, current: e.target.value }))}
                      placeholder="••••••••"
                    />
                  </div>
                  <div className="field">
                    <label>Mật khẩu mới</label>
                    <input
                      type="password"
                      value={pwdForm.next}
                      onChange={(e) => setPwdForm((f) => ({ ...f, next: e.target.value }))}
                      placeholder="••••••••"
                    />
                  </div>
                  <div className="field">
                    <label>Xác nhận mật khẩu mới</label>
                    <input
                      type="password"
                      value={pwdForm.confirm}
                      onChange={(e) => setPwdForm((f) => ({ ...f, confirm: e.target.value }))}
                      placeholder="••••••••"
                    />
                  </div>
                  <button
                    className="btn btn-danger"
                    onClick={doChangePassword}
                    disabled={profileBusy || loading || !pwdForm.current || !pwdForm.next}
                  >
                    {profileBusy ? <span className="spinner" style={{ borderTopColor: 'var(--danger)' }} /> : null}
                    Đổi mật khẩu
                  </button>
                </div>
              </div>
            </div>
          )}
        </div>
      </main>

      {/* Toast */}
      {toast && (
        <div
          style={{
            position: 'fixed', bottom: 24, right: 24, zIndex: 9999,
            background: toast.type === 'error' ? '#1e1b1b' : '#0f2037',
            color: toast.type === 'error' ? '#fca5a5' : '#bfdbfe',
            padding: '12px 20px', borderRadius: 12,
            boxShadow: '0 8px 24px rgba(0,0,0,.3)',
            fontSize: 14, fontWeight: 500,
            animation: 'fadeUp .25s ease',
            maxWidth: 360,
          }}
        >
          {toast.type === 'error' ? '⚠️ ' : '✅ '}{toast.msg}
        </div>
      )}
    </div>
  )
}
