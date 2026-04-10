import { useState, useEffect, useCallback, useRef } from 'react'
import { useNavigate } from 'react-router-dom'
import * as signalR from '@microsoft/signalr'
import { auth, driversApi, driverTripsApi, notificationsApi, getToken } from '../api'
import TripChat from '../components/TripChat'
import TripLiveMap from '../components/TripLiveMap'
import type { UserDto, TripDto, NotificationDto, RideOfferPayload, DriverDto, VehicleDto, TripMessageDto, DriverDailyEarningsDto } from '../types'

const API_BASE = import.meta.env.VITE_API_BASE_URL ?? 'http://localhost:8080/api/v1'
const HUB_URL = API_BASE.replace('/api/v1', '') + '/hubs/ride'

// ─── Vehicle type config ──────────────────────────────────────────────────────

const VEHICLE_TYPES = [
  { value: 'ElectricBike', label: 'Xe điện', icon: '⚡', desc: '1 chỗ',  models: ['Evo200', 'Feliz S', 'Klara S', 'Theon S'], seatCount: 1 },
  { value: 'Seat4',        label: '4 chỗ',   icon: '🚗', desc: '4 chỗ',  models: ['VF 5', 'VF 6', 'VF e34'],                  seatCount: 4 },
  { value: 'Seat7',        label: '7 chỗ',   icon: '🚙', desc: '7 chỗ',  models: ['VF 7', 'VF 8'],                            seatCount: 7 },
  { value: 'Seat9',        label: '9 chỗ',   icon: '🚌', desc: '9 chỗ',  models: ['VF 9'],                                    seatCount: 9 },
]

// ─── Helpers ──────────────────────────────────────────────────────────────────

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

function getCurrentPosition(options?: PositionOptions): Promise<GeolocationPosition> {
  return new Promise((resolve, reject) => {
    navigator.geolocation.getCurrentPosition(resolve, reject, options)
  })
}

function tripStatusLabel(s: string) {
  const map: Record<string, string> = {
    Accepted: 'Đã nhận', DriverEnRoute: 'Đang đến đón',
    DriverArrived: 'Đã đến điểm đón', InProgress: 'Đang chạy',
    Completed: 'Hoàn thành', Cancelled: 'Đã huỷ',
  }
  return map[s] ?? s
}

function tripStatusBadge(s: string) {
  const cls: Record<string, string> = {
    Accepted: 'badge-info', DriverEnRoute: 'badge-warning',
    DriverArrived: 'badge-warning', InProgress: 'badge-info',
    Completed: 'badge-success', Cancelled: 'badge-danger',
  }
  return <span className={`badge ${cls[s] ?? 'badge-neutral'}`}>{tripStatusLabel(s)}</span>
}

function btnStyle(bg: string): React.CSSProperties {
  return {
    width: '100%', padding: '14px', borderRadius: 10, border: 'none',
    background: bg, color: '#fff', cursor: 'pointer', fontSize: 15, fontWeight: 700,
  }
}

function vehicleTypeBySeatCount(seatCount: number): VehicleUpdateDraft['vehicleType'] {
  if (seatCount === 1) return 'ElectricBike'
  if (seatCount === 4) return 'Seat4'
  if (seatCount === 7) return 'Seat7'
  return 'Seat9'
}

function seatCountByVehicleType(vehicleType: VehicleUpdateDraft['vehicleType']): number {
  if (vehicleType === 'ElectricBike') return 1
  if (vehicleType === 'Seat4') return 4
  if (vehicleType === 'Seat7') return 7
  return 9
}

// ─── Types ────────────────────────────────────────────────────────────────────

type PageState = 'loading' | 'no-profile' | 'pending' | 'active'
type Tab = 'overview' | 'current' | 'history' | 'notifications' | 'profile'
type WizardStep = 1 | 2
interface Props { user: UserDto; onLogout: () => void; driverProfile?: DriverDto | null }

type VehicleUpdateDraft = {
  make: string
  model: string
  year: number
  color: string
  plateNumber: string
  seatCount: number
  imageUrl: string
  vehicleType: 'ElectricBike' | 'Seat4' | 'Seat7' | 'Seat9'
}

function normalizeVehicleType(type: string): VehicleUpdateDraft['vehicleType'] {
  if (type === 'ElectricBike' || type === 'Seat4' || type === 'Seat7' || type === 'Seat9') {
    return type
  }
  return 'Seat4'
}

const LOCATION_REFRESH_MS = 3000

// ─── Sidebar shared component ─────────────────────────────────────────────────

function Sidebar({ displayName, user, tab, setTab, onLogout, navItems }: {
  displayName: string
  user: UserDto
  tab?: string
  setTab?: (t: Tab) => void
  onLogout: () => void
  navItems?: { key: Tab; icon: string; label: string; badge?: number }[]
}) {
  return (
    <aside style={{
      width: 220, background: '#1e293b', display: 'flex', flexDirection: 'column',
      padding: '24px 0', borderRight: '1px solid #334155', flexShrink: 0,
    }}>
      <div style={{ padding: '0 20px 24px', borderBottom: '1px solid #334155' }}>
        <div style={{ display: 'flex', alignItems: 'center', gap: 10 }}>
          <div style={{
            width: 36, height: 36, borderRadius: 10,
            background: 'linear-gradient(135deg, #10b981, #059669)',
            display: 'flex', alignItems: 'center', justifyContent: 'center',
            fontSize: 18, fontWeight: 700, color: '#fff',
          }}>G</div>
          <div>
            <div style={{ fontWeight: 700, fontSize: 15, color: '#f1f5f9' }}>Gokt</div>
            <div style={{ fontSize: 11, color: '#10b981', fontWeight: 600 }}>Tài xế</div>
          </div>
        </div>
      </div>

      {navItems && setTab && (
        <nav style={{ flex: 1, padding: '16px 12px' }}>
          <div style={{ fontSize: 11, color: '#64748b', fontWeight: 600, textTransform: 'uppercase', padding: '0 8px 8px' }}>MENU</div>
          {navItems.map(item => (
            <button key={item.key} onClick={() => setTab(item.key)} style={{
              width: '100%', display: 'flex', alignItems: 'center', gap: 10,
              padding: '10px 12px', borderRadius: 8, border: 'none', cursor: 'pointer',
              background: tab === item.key ? 'rgba(16,185,129,0.15)' : 'transparent',
              color: tab === item.key ? '#10b981' : '#94a3b8',
              fontWeight: tab === item.key ? 600 : 400, fontSize: 14, textAlign: 'left',
              marginBottom: 2, transition: 'all 0.15s', fontFamily: 'inherit',
            }}>
              <span style={{ fontSize: 16 }}>{item.icon}</span>
              <span style={{ flex: 1 }}>{item.label}</span>
              {!!item.badge && (
                <span style={{
                  background: '#ef4444', color: '#fff', borderRadius: 10,
                  fontSize: 11, fontWeight: 700, padding: '1px 6px',
                }}>{item.badge}</span>
              )}
            </button>
          ))}
        </nav>
      )}

      <div style={{ flex: navItems ? 0 : 1 }} />

      <div style={{ padding: '16px 20px', borderTop: '1px solid #334155' }}>
        <div style={{ display: 'flex', alignItems: 'center', gap: 10, marginBottom: 12 }}>
          <div style={{
            width: 34, height: 34, borderRadius: '50%',
            background: 'linear-gradient(135deg, #10b981, #059669)',
            display: 'flex', alignItems: 'center', justifyContent: 'center',
            fontSize: 14, fontWeight: 700, color: '#fff', flexShrink: 0,
          }}>{displayName[0]?.toUpperCase()}</div>
          <div style={{ overflow: 'hidden' }}>
            <div style={{ fontSize: 13, fontWeight: 600, color: '#f1f5f9', whiteSpace: 'nowrap', overflow: 'hidden', textOverflow: 'ellipsis' }}>{displayName}</div>
            <div style={{ fontSize: 11, color: '#64748b' }}>{user.email}</div>
          </div>
        </div>
        <button onClick={onLogout} style={{
          width: '100%', padding: '8px', borderRadius: 8, border: '1px solid #334155',
          background: 'transparent', color: '#64748b', cursor: 'pointer', fontSize: 13, fontFamily: 'inherit',
        }}>Đăng xuất</button>
      </div>
    </aside>
  )
}

// ─── State 1: Onboarding Wizard ───────────────────────────────────────────────

function OnboardingWizard({ user, onComplete, onLogout }: {
  user: UserDto
  onComplete: (profile: DriverDto) => void
  onLogout: () => void
}) {
  const [step, setStep] = useState<WizardStep>(1)
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState('')

  // Step 1 — License
  const [license, setLicense] = useState({ number: '', expiry: '' })

  // Step 2 — Vehicle
  const [vehicleType, setVehicleType] = useState('Seat4')
  const vehicleConfig = VEHICLE_TYPES.find(v => v.value === vehicleType)!
  const [vehicle, setVehicle] = useState({
    model: vehicleConfig.models[0],
    year: new Date().getFullYear(),
    color: '',
    plateNumber: '',
  })

  // When vehicleType changes, reset model to first in list
  useEffect(() => {
    const cfg = VEHICLE_TYPES.find(v => v.value === vehicleType)!
    setVehicle(v => ({ ...v, model: cfg.models[0] }))
  }, [vehicleType])

  const displayName = [user.firstName, user.lastName].filter(Boolean).join(' ') || user.email

  const submitStep1 = async (e: React.FormEvent) => {
    e.preventDefault()
    setError('')
    setBusy(true)
    try {
      await driversApi.register(license.number, license.expiry)
      setStep(2)
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Đăng ký thất bại')
    } finally {
      setBusy(false)
    }
  }

  const submitStep2 = async (e: React.FormEvent) => {
    e.preventDefault()
    setError('')
    setBusy(true)
    try {
      const cfg = VEHICLE_TYPES.find(v => v.value === vehicleType)!
      await driversApi.addVehicle({
        make: 'VinFast',
        model: vehicle.model,
        year: vehicle.year,
        color: vehicle.color,
        plateNumber: vehicle.plateNumber.toUpperCase(),
        seatCount: cfg.seatCount,
        vehicleType,
      })
      // Fetch updated profile
      const profile = await driversApi.me()
      if (profile) onComplete(profile)
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Thêm xe thất bại')
    } finally {
      setBusy(false)
    }
  }

  const setVeh = (k: keyof typeof vehicle) => (e: React.ChangeEvent<HTMLInputElement | HTMLSelectElement>) =>
    setVehicle(v => ({ ...v, [k]: k === 'year' ? parseInt(e.target.value) || v.year : e.target.value }))

  return (
    <div style={{ display: 'flex', minHeight: '100vh', background: '#0f172a', color: '#e2e8f0' }}>
      <Sidebar displayName={displayName} user={user} onLogout={onLogout} />

      <main style={{ flex: 1, display: 'flex', alignItems: 'center', justifyContent: 'center', padding: 32 }}>
        <div style={{ width: '100%', maxWidth: 520 }}>

          {/* Progress */}
          <div style={{ display: 'flex', alignItems: 'center', gap: 12, marginBottom: 32 }}>
            {[1, 2].map(s => (
              <div key={s} style={{ display: 'flex', alignItems: 'center', gap: 8, flex: 1 }}>
                <div style={{
                  width: 32, height: 32, borderRadius: '50%', flexShrink: 0,
                  display: 'flex', alignItems: 'center', justifyContent: 'center',
                  fontWeight: 700, fontSize: 14,
                  background: step >= s ? '#10b981' : '#334155',
                  color: step >= s ? '#fff' : '#64748b',
                }}>{s}</div>
                <div>
                  <div style={{ fontSize: 12, color: step >= s ? '#10b981' : '#64748b', fontWeight: 600 }}>
                    {s === 1 ? 'Bằng lái xe' : 'Thông tin xe'}
                  </div>
                </div>
                {s < 2 && (
                  <div style={{ flex: 1, height: 2, background: step > s ? '#10b981' : '#334155', marginLeft: 8 }} />
                )}
              </div>
            ))}
          </div>

          <div style={{ background: '#1e293b', borderRadius: 20, padding: 36, border: '1px solid #334155' }}>
            <h1 style={{ margin: '0 0 8px', color: '#f1f5f9', fontSize: 22, fontWeight: 700 }}>
              {step === 1 ? 'Đăng ký làm tài xế' : 'Thêm phương tiện'}
            </h1>
            <p style={{ margin: '0 0 28px', color: '#64748b', fontSize: 14 }}>
              {step === 1 ? 'Nhập thông tin bằng lái xe của bạn' : 'Chọn loại xe và điền thông tin phương tiện'}
            </p>

            {error && (
              <div style={{ background: 'rgba(239,68,68,.1)', border: '1px solid #ef4444', borderRadius: 10, padding: '12px 16px', marginBottom: 20, color: '#fca5a5', fontSize: 14 }}>
                {error}
              </div>
            )}

            {/* ── Step 1: License ── */}
            {step === 1 && (
              <form onSubmit={submitStep1}>
                <div className="field">
                  <label style={{ color: '#94a3b8' }}>Số bằng lái</label>
                  <input
                    type="text" required
                    value={license.number}
                    onChange={e => setLicense(l => ({ ...l, number: e.target.value }))}
                    placeholder="VD: 123456789012"
                    style={{ background: '#0f172a', borderColor: '#334155', color: '#f1f5f9' }}
                  />
                </div>
                <div className="field">
                  <label style={{ color: '#94a3b8' }}>Ngày hết hạn bằng lái</label>
                  <input
                    type="date" required
                    value={license.expiry}
                    onChange={e => setLicense(l => ({ ...l, expiry: e.target.value }))}
                    min={new Date().toISOString().split('T')[0]}
                    style={{ background: '#0f172a', borderColor: '#334155', color: '#f1f5f9' }}
                  />
                </div>
                <button type="submit" disabled={busy} style={{ ...btnStyle('#10b981'), marginTop: 8 }}>
                  {busy ? 'Đang gửi...' : 'Tiếp theo →'}
                </button>
              </form>
            )}

            {/* ── Step 2: Vehicle ── */}
            {step === 2 && (
              <form onSubmit={submitStep2}>
                {/* Vehicle type selector */}
                <div className="field">
                  <label style={{ color: '#94a3b8' }}>Loại phương tiện</label>
                  <div style={{ display: 'grid', gridTemplateColumns: 'repeat(4, 1fr)', gap: 8, marginTop: 6 }}>
                    {VEHICLE_TYPES.map(vt => (
                      <button
                        key={vt.value} type="button"
                        onClick={() => setVehicleType(vt.value)}
                        style={{
                          padding: '12px 6px', borderRadius: 10, border: `2px solid ${vehicleType === vt.value ? '#10b981' : '#334155'}`,
                          background: vehicleType === vt.value ? 'rgba(16,185,129,0.1)' : 'transparent',
                          color: vehicleType === vt.value ? '#10b981' : '#64748b',
                          cursor: 'pointer', textAlign: 'center', fontFamily: 'inherit',
                          transition: 'all 0.15s',
                        }}
                      >
                        <div style={{ fontSize: 22, marginBottom: 4 }}>{vt.icon}</div>
                        <div style={{ fontSize: 12, fontWeight: 600 }}>{vt.label}</div>
                        <div style={{ fontSize: 10 }}>{vt.desc}</div>
                      </button>
                    ))}
                  </div>
                </div>

                <div className="field">
                  <label style={{ color: '#94a3b8' }}>Hãng xe</label>
                  <input
                    type="text" value="VinFast" disabled
                    style={{ background: '#0f172a', borderColor: '#334155', color: '#64748b' }}
                  />
                  <p style={{ fontSize: 12, color: '#475569', marginTop: 4 }}>Chỉ chấp nhận xe VinFast điện</p>
                </div>

                <div className="field">
                  <label style={{ color: '#94a3b8' }}>Model xe</label>
                  <select
                    value={vehicle.model}
                    onChange={e => setVehicle(v => ({ ...v, model: e.target.value }))}
                    style={{ background: '#0f172a', borderColor: '#334155', color: '#f1f5f9' }}
                  >
                    {vehicleConfig.models.map(m => (
                      <option key={m} value={m}>{m}</option>
                    ))}
                  </select>
                </div>

                <div className="field-row">
                  <div className="field" style={{ marginBottom: 0 }}>
                    <label style={{ color: '#94a3b8' }}>Năm sản xuất</label>
                    <input
                      type="number" required min={2000} max={new Date().getFullYear() + 1}
                      value={vehicle.year}
                      onChange={setVeh('year')}
                      style={{ background: '#0f172a', borderColor: '#334155', color: '#f1f5f9' }}
                    />
                  </div>
                  <div className="field" style={{ marginBottom: 0 }}>
                    <label style={{ color: '#94a3b8' }}>Màu xe</label>
                    <input
                      type="text" required placeholder="VD: Trắng, Đen"
                      value={vehicle.color}
                      onChange={setVeh('color')}
                      style={{ background: '#0f172a', borderColor: '#334155', color: '#f1f5f9' }}
                    />
                  </div>
                </div>

                <div className="field mt-4">
                  <label style={{ color: '#94a3b8' }}>Biển số xe</label>
                  <input
                    type="text" required placeholder="VD: 51G-123.45"
                    value={vehicle.plateNumber}
                    onChange={e => setVehicle(v => ({ ...v, plateNumber: e.target.value.toUpperCase() }))}
                    style={{ background: '#0f172a', borderColor: '#334155', color: '#f1f5f9' }}
                  />
                </div>

                <div style={{ display: 'flex', gap: 10, marginTop: 8 }}>
                  <button type="button" onClick={() => setStep(1)} style={{
                    flex: 1, padding: '14px', borderRadius: 10, border: '1px solid #334155',
                    background: 'transparent', color: '#94a3b8', cursor: 'pointer', fontSize: 14, fontFamily: 'inherit',
                  }}>← Quay lại</button>
                  <button type="submit" disabled={busy} style={{ ...btnStyle('#10b981'), flex: 2 }}>
                    {busy ? 'Đang gửi...' : 'Hoàn tất đăng ký'}
                  </button>
                </div>
              </form>
            )}
          </div>
        </div>
      </main>
    </div>
  )
}

// ─── State 2: Pending Approval ────────────────────────────────────────────────

function PendingScreen({ user, profile, onLogout }: {
  user: UserDto
  profile: DriverDto
  onLogout: () => void
}) {
  const displayName = [user.firstName, user.lastName].filter(Boolean).join(' ') || user.email

  return (
    <div style={{ display: 'flex', minHeight: '100vh', background: '#0f172a', color: '#e2e8f0' }}>
      <Sidebar displayName={displayName} user={user} onLogout={onLogout} />

      <main style={{ flex: 1, display: 'flex', alignItems: 'center', justifyContent: 'center', padding: 32 }}>
        <div style={{ textAlign: 'center', maxWidth: 480 }}>
          <div style={{
            width: 80, height: 80, borderRadius: '50%',
            background: 'rgba(245,158,11,0.15)', border: '2px solid #f59e0b',
            display: 'flex', alignItems: 'center', justifyContent: 'center',
            fontSize: 36, margin: '0 auto 24px',
          }}>⏳</div>

          <h1 style={{ fontSize: 24, fontWeight: 700, color: '#f1f5f9', marginBottom: 12 }}>
            Hồ sơ đang được xét duyệt
          </h1>
          <p style={{ color: '#64748b', fontSize: 15, lineHeight: 1.7, marginBottom: 32 }}>
            Admin sẽ xem xét thông tin của bạn và phê duyệt trong thời gian sớm nhất.
            Bạn sẽ có thể bắt đầu nhận cuốc ngay sau khi được duyệt.
          </p>

          {/* Driver info */}
          <div style={{ background: '#1e293b', borderRadius: 16, padding: 24, border: '1px solid #334155', textAlign: 'left', marginBottom: 24 }}>
            <div style={{ fontSize: 13, color: '#64748b', marginBottom: 16, fontWeight: 600, textTransform: 'uppercase', letterSpacing: '.06em' }}>
              Thông tin đã đăng ký
            </div>
            {[
              { label: 'Mã tài xế', value: profile.driverCode ?? '—' },
              { label: 'Trạng thái', value: 'Chờ duyệt' },
              { label: 'Số xe', value: profile.vehicles?.length ? `${profile.vehicles.length} xe` : 'Chưa có xe' },
            ].map(row => (
              <div key={row.label} style={{
                display: 'flex', justifyContent: 'space-between', alignItems: 'center',
                padding: '10px 0', borderBottom: '1px solid #334155',
              }}>
                <span style={{ color: '#64748b', fontSize: 14 }}>{row.label}</span>
                <span style={{ color: '#f1f5f9', fontSize: 14, fontWeight: 600 }}>{row.value}</span>
              </div>
            ))}
            {profile.vehicles?.map((v: VehicleDto) => (
              <div key={v.id} style={{ marginTop: 12, padding: '12px 14px', background: '#0f172a', borderRadius: 10 }}>
                <div style={{ fontWeight: 600, color: '#f1f5f9', marginBottom: 4 }}>
                  {v.make} {v.model} • {v.color}
                </div>
                <div style={{ fontSize: 12, color: '#64748b' }}>
                  {v.plateNumber} · {v.year} · {v.seatCount} chỗ
                </div>
              </div>
            ))}
          </div>

          <div style={{
            background: 'rgba(245,158,11,0.08)', border: '1px solid rgba(245,158,11,0.3)',
            borderRadius: 12, padding: '14px 20px', fontSize: 13, color: '#fbbf24',
          }}>
            Thông thường quá trình xét duyệt mất 24–48 giờ. Vui lòng đảm bảo thông tin bằng lái và xe chính xác.
          </div>
        </div>
      </main>
    </div>
  )
}

// ─── State 3: Active Driver Dashboard (original code refactored) ──────────────

function ActiveDashboard({ user, onLogout, driverProfile }: Props) {
  const nav = useNavigate()
  const [profile, setProfile] = useState<DriverDto | null>(driverProfile ?? null)
  const [tab, setTab] = useState<Tab>('overview')
  const [isOnline, setIsOnline] = useState(false)
  const [onlineLoading, setOnlineLoading] = useState(false)
  const [activeTrip, setActiveTrip] = useState<TripDto | null>(null)
  const [trips, setTrips] = useState<TripDto[]>([])
  const [notifs, setNotifs] = useState<NotificationDto[]>([])
  const [offer, setOffer] = useState<RideOfferPayload | null>(null)
  const [offerLoading, setOfferLoading] = useState(false)
  const [toast, setToast] = useState<{ msg: string; type: 'success' | 'error' } | null>(null)
  const [loading, setLoading] = useState(false)
  const [currentPos, setCurrentPos] = useState<{ lat: number; lng: number } | null>(null)
  const hubRef = useRef<signalR.HubConnection | null>(null)
  const lastGpsErrorAtRef = useRef(0)
  const [hubConnected, setHubConnected] = useState(false)
  const [distanceInput, setDistanceInput] = useState('')
  const [chatMessages, setChatMessages] = useState<TripMessageDto[]>([])
  const [customerPos, setCustomerPos] = useState<{ lat: number; lng: number } | null>(null)
  const [dailyEarnings, setDailyEarnings] = useState<DriverDailyEarningsDto | null>(null)
  const [vehicleDraftById, setVehicleDraftById] = useState<Record<string, VehicleUpdateDraft>>({})

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

  const loadData = useCallback(() => run(async () => {
    const [history, ns, earnings] = await Promise.all([
      driversApi.trips(),
      notificationsApi.list(),
      driversApi.dailyEarnings(),
    ])
    setTrips(history)
    setNotifs(ns)
    setDailyEarnings(earnings)
    const active = history.find(t =>
      ['Accepted', 'DriverEnRoute', 'DriverArrived', 'InProgress'].includes(t.status)
    ) ?? null
    setActiveTrip(active)
    if (active) setTab('current')
  }), [run])

  useEffect(() => { loadData() }, [loadData])

  useEffect(() => {
    setProfile(driverProfile ?? null)
  }, [driverProfile])

  useEffect(() => {
    if (!profile) return
    const drafts: Record<string, VehicleUpdateDraft> = Object.fromEntries(
      profile.vehicles.map(v => [v.id, {
        make: v.make,
        model: v.model,
        year: v.year,
        color: v.color,
        plateNumber: v.plateNumber,
        seatCount: v.seatCount,
        imageUrl: v.imageUrl ?? '',
        vehicleType: normalizeVehicleType(v.vehicleType),
      }]),
    )
    setVehicleDraftById(drafts)
  }, [profile])

  useEffect(() => {
    let stopped = false
    const conn = new signalR.HubConnectionBuilder()
      .withUrl(HUB_URL, { accessTokenFactory: () => getToken() })
      .withAutomaticReconnect()
      .configureLogging(signalR.LogLevel.Warning)
      .build()

    conn.on('ReceiveRideOffer', (payload: RideOfferPayload) => setOffer(payload))
    conn.on('RideTaken', () => { setOffer(null); showToast('Cuốc xe đã được tài xế khác nhận', 'error') })
    conn.on('RideAccepted', (trip: TripDto) => {
      setActiveTrip(trip); setOffer(null); setTab('current'); showToast('Đã nhận cuốc thành công!')
    })
    conn.on('ReceiveMessage', (msg: TripMessageDto) => {
      if (!stopped) setChatMessages(prev => [...prev, msg])
    })
    conn.on('MessageSent', (msg: TripMessageDto) => {
      if (!stopped) setChatMessages(prev => [...prev, msg])
    })
    conn.on('CustomerLocationUpdate', (p: { latitude: number; longitude: number }) => {
      if (!stopped) setCustomerPos({ lat: p.latitude, lng: p.longitude })
    })
    conn.on('Error', (msg: string) => showToast(msg, 'error'))
    conn.onreconnecting(() => { setHubConnected(false); showToast('Mất kết nối realtime, đang thử kết nối lại...', 'error') })
    conn.onreconnected(() => { setHubConnected(true); showToast('Đã kết nối lại realtime') })
    conn.onclose(() => { if (!stopped) { setHubConnected(false); showToast('Kết nối realtime đã đóng', 'error') } })

    hubRef.current = conn
    conn.start()
      .then(() => { if (!stopped) setHubConnected(true) })
      .catch(() => { if (!stopped) showToast('Không thể kết nối realtime', 'error') })

    return () => {
      stopped = true
      setHubConnected(false)
      conn.stop().catch(() => {})
    }
  }, [])

  useEffect(() => {
    if (!navigator.geolocation) {
      showToast('Thiết bị không hỗ trợ GPS', 'error')
      return
    }

    let cancelled = false
    let busy = false

    const tick = async () => {
      if (busy) return
      busy = true
      try {
        const pos = await getCurrentPosition({ enableHighAccuracy: true, timeout: 8000, maximumAge: 1000 })
        if (cancelled) return
        const lat = pos.coords.latitude
        const lng = pos.coords.longitude
        setCurrentPos({ lat, lng })
        if (isOnline) await driversApi.updateLocation(lat, lng)
      } catch {
        if (!cancelled) {
          const now = Date.now()
          if (now - lastGpsErrorAtRef.current > 15000) {
            showToast('Không thể cập nhật vị trí tài xế', 'error')
            lastGpsErrorAtRef.current = now
          }
        }
      } finally {
        busy = false
      }
    }

    tick()
    const timer = window.setInterval(tick, LOCATION_REFRESH_MS)
    return () => {
      cancelled = true
      window.clearInterval(timer)
    }
  }, [isOnline])

  useEffect(() => {
    if (!activeTrip) return
    const fallback = activeTrip.estimatedDistanceKm ?? activeTrip.actualDistanceKm ?? null
    if (fallback != null) {
      setDistanceInput(String(fallback))
    }
  }, [activeTrip])

  const toggleOnline = async () => {
    setOnlineLoading(true)
    try {
      await driversApi.toggleOnline(!isOnline)
      setIsOnline(v => !v)
      showToast(!isOnline ? 'Bạn đang online, sẵn sàng nhận cuốc' : 'Bạn đã offline')
    } catch (e) {
      showToast(e instanceof Error ? e.message : 'Lỗi', 'error')
    } finally { setOnlineLoading(false) }
  }

  const acceptOffer = async () => {
    if (!offer || !hubRef.current) return
    setOfferLoading(true)
    try { await hubRef.current.invoke('AcceptRide', offer.rideRequestId) }
    catch (e) { showToast(e instanceof Error ? e.message : 'Không thể nhận cuốc', 'error') }
    finally { setOfferLoading(false) }
  }

  const declineOffer = async () => {
    if (!offer || !hubRef.current) return
    try { await hubRef.current.invoke('DeclineRide', offer.rideRequestId) }
    finally { setOffer(null) }
  }

  const updateTripStatus = (status: string) => run(async () => {
    if (!activeTrip) return
    const updated = await driverTripsApi.updateStatus(activeTrip.id, status)
    setActiveTrip(updated)
    showToast(`Cập nhật: ${tripStatusLabel(status)}`)
  })

  const completeTrip = () => run(async () => {
    if (!activeTrip) return
    const parsedDistance = Number.parseFloat(distanceInput)
    if (!Number.isFinite(parsedDistance) || parsedDistance <= 0) {
      throw new Error('Vui lòng nhập quãng đường thực tế hợp lệ (> 0 km)')
    }

    const updated = await driverTripsApi.complete(activeTrip.id, parsedDistance)
    setActiveTrip(updated)
    setCustomerPos(null)
    setTrips(ts => ts.map(t => t.id === updated.id ? updated : t))
    showToast('Hoàn thành chuyến đi!')
    setTimeout(() => { setActiveTrip(null); setTab('overview') }, 1500)
  })

  const handleLogout = () => run(async () => {
    await auth.logout()
    onLogout()
    nav('/login', { replace: true })
  })

  const unreadCount = notifs.filter(n => !n.isRead).length
  const displayName = [user.firstName, user.lastName].filter(Boolean).join(' ') || user.email

  const setVehicleDraftField = <K extends keyof VehicleUpdateDraft>(vehicleId: string, field: K, value: VehicleUpdateDraft[K]) => {
    setVehicleDraftById(prev => ({
      ...prev,
      [vehicleId]: {
        ...(prev[vehicleId] ?? {
          make: '', model: '', year: new Date().getFullYear(), color: '',
          plateNumber: '', seatCount: 4, imageUrl: '', vehicleType: 'Seat4' as const,
        }),
        [field]: value,
      },
    }))
  }

  const submitVehicleUpdateRequest = (vehicle: VehicleDto) => run(async () => {
    const draft = vehicleDraftById[vehicle.id] ?? {
      make: vehicle.make,
      model: vehicle.model,
      year: vehicle.year,
      color: vehicle.color,
      plateNumber: vehicle.plateNumber,
      seatCount: vehicle.seatCount,
      imageUrl: vehicle.imageUrl ?? '',
      vehicleType: normalizeVehicleType(vehicle.vehicleType),
    }

    const seatCount = Number(draft.seatCount)
    if (![1, 4, 7, 9].includes(seatCount)) {
      throw new Error('Số chỗ chỉ được là 1, 4, 7 hoặc 9')
    }
    if (!draft.make.trim() || !draft.model.trim() || !draft.color.trim() || !draft.plateNumber.trim()) {
      throw new Error('Vui lòng nhập đầy đủ hãng xe, dòng xe, màu xe và biển số')
    }

    const updated = await driversApi.requestVehicleUpdate(vehicle.id, {
      make: draft.make.trim(),
      model: draft.model.trim(),
      year: Number(draft.year),
      color: draft.color.trim(),
      plateNumber: draft.plateNumber.trim().toUpperCase(),
      seatCount,
      imageUrl: draft.imageUrl.trim() || undefined,
      vehicleType: vehicleTypeBySeatCount(seatCount),
    })

    setProfile(prev => {
      if (!prev) return prev
      return {
        ...prev,
        vehicles: prev.vehicles.map(v => (v.id === updated.id ? updated : v)),
      }
    })
    setVehicleDraftById(prev => ({
      ...prev,
      [updated.id]: {
        make: updated.make,
        model: updated.model,
        year: updated.year,
        color: updated.color,
        plateNumber: updated.plateNumber,
        seatCount: updated.seatCount,
        imageUrl: updated.imageUrl ?? '',
        vehicleType: normalizeVehicleType(updated.vehicleType),
      },
    }))
    showToast('Đã gửi yêu cầu cập nhật xe. Chờ admin duyệt.')
  })

  const navItems: { key: Tab; icon: string; label: string; badge?: number }[] = [
    { key: 'overview',      icon: '🏠', label: 'Tổng quan' },
    { key: 'current',       icon: '🚗', label: 'Chuyến hiện tại', badge: activeTrip ? 1 : undefined },
    { key: 'history',       icon: '📋', label: 'Lịch sử' },
    { key: 'notifications', icon: '🔔', label: 'Thông báo', badge: unreadCount || undefined },
    { key: 'profile',       icon: '👤', label: 'Hồ sơ' },
  ]

  return (
    <div style={{ display: 'flex', minHeight: '100vh', background: '#0f172a', color: '#e2e8f0' }}>
      <Sidebar displayName={displayName} user={user} tab={tab} setTab={setTab} onLogout={handleLogout} navItems={navItems} />

      <main style={{ flex: 1, padding: '32px', overflowY: 'auto' }}>
        {/* Hub connection badge — top right corner */}
        <div style={{ position: 'fixed', top: 16, right: 20, zIndex: 9998 }}>
          <span style={{
            display: 'inline-flex', alignItems: 'center', gap: 6,
            background: hubConnected ? '#064e3b' : '#1c1917',
            border: `1px solid ${hubConnected ? '#10b981' : '#44403c'}`,
            color: hubConnected ? '#10b981' : '#78716c',
            padding: '4px 12px', borderRadius: 20, fontSize: 12, fontWeight: 600,
          }}>
            <span style={{
              width: 7, height: 7, borderRadius: '50%',
              background: hubConnected ? '#10b981' : '#78716c',
              boxShadow: hubConnected ? '0 0 6px #10b981' : 'none',
            }} />
            {hubConnected ? 'Realtime' : 'Mất kết nối'}
          </span>
        </div>

        {toast && (
          <div style={{
            position: 'fixed', top: 20, right: 20, zIndex: 9999,
            background: toast.type === 'success' ? '#10b981' : '#ef4444',
            color: '#fff', padding: '12px 20px', borderRadius: 10,
            fontWeight: 600, fontSize: 14, boxShadow: '0 4px 20px rgba(0,0,0,0.3)',
          }}>{toast.msg}</div>
        )}

        {/* Offer modal */}
        {offer && (
          <div style={{
            position: 'fixed', inset: 0, background: 'rgba(0,0,0,0.7)', zIndex: 1000,
            display: 'flex', alignItems: 'center', justifyContent: 'center',
          }}>
            <div style={{
              background: '#1e293b', borderRadius: 16, padding: 28, width: 380,
              border: '1px solid #334155', boxShadow: '0 20px 60px rgba(0,0,0,0.5)',
            }}>
              <div style={{ display: 'flex', justifyContent: 'space-between', marginBottom: 16 }}>
                <h3 style={{ margin: 0, color: '#f1f5f9', fontSize: 18 }}>Cuốc xe mới!</h3>
                <span style={{ background: '#10b981', color: '#fff', padding: '2px 10px', borderRadius: 20, fontSize: 12, fontWeight: 700 }}>
                  {offer.vehicleType}
                </span>
              </div>
              <div style={{ background: '#0f172a', borderRadius: 10, padding: 16, marginBottom: 16 }}>
                <div style={{ marginBottom: 10 }}>
                  <div style={{ fontSize: 11, color: '#10b981', fontWeight: 600, marginBottom: 4 }}>ĐIỂM ĐÓN</div>
                  <div style={{ fontSize: 14, color: '#e2e8f0' }}>{offer.pickupAddress}</div>
                </div>
                <div style={{ height: 1, background: '#1e293b', margin: '10px 0' }} />
                <div>
                  <div style={{ fontSize: 11, color: '#f59e0b', fontWeight: 600, marginBottom: 4 }}>ĐIỂM TRẢ</div>
                  <div style={{ fontSize: 14, color: '#e2e8f0' }}>{offer.dropoffAddress}</div>
                </div>
              </div>
              <div style={{ display: 'flex', gap: 16, marginBottom: 20 }}>
                <div style={{ flex: 1, background: '#0f172a', borderRadius: 8, padding: '10px 14px', textAlign: 'center' }}>
                  <div style={{ fontSize: 11, color: '#64748b' }}>Khoảng cách</div>
                  <div style={{ fontSize: 16, fontWeight: 700, color: '#f1f5f9' }}>{offer.estimatedDistanceKm?.toFixed(1) ?? '?'} km</div>
                </div>
                <div style={{ flex: 1, background: '#0f172a', borderRadius: 8, padding: '10px 14px', textAlign: 'center' }}>
                  <div style={{ fontSize: 11, color: '#64748b' }}>Dự kiến</div>
                  <div style={{ fontSize: 16, fontWeight: 700, color: '#10b981' }}>{fmtCurrency(offer.estimatedFare)}</div>
                </div>
              </div>
              <div style={{ display: 'flex', gap: 10 }}>
                <button onClick={declineOffer} style={{
                  flex: 1, padding: '12px', borderRadius: 10, border: '1px solid #334155',
                  background: 'transparent', color: '#94a3b8', cursor: 'pointer', fontSize: 15, fontWeight: 600, fontFamily: 'inherit',
                }}>Từ chối</button>
                <button onClick={acceptOffer} disabled={offerLoading} style={{
                  flex: 2, padding: '12px', borderRadius: 10, border: 'none',
                  background: '#10b981', color: '#fff', cursor: 'pointer', fontSize: 15, fontWeight: 700, fontFamily: 'inherit',
                }}>{offerLoading ? 'Đang nhận...' : 'Nhận cuốc'}</button>
              </div>
            </div>
          </div>
        )}

        {loading && <div style={{ textAlign: 'center', padding: 40, color: '#64748b' }}>Đang tải...</div>}

        {/* Overview */}
        {tab === 'overview' && (
          <div>
            <h2 style={{ margin: '0 0 24px', color: '#f1f5f9' }}>Tổng quan</h2>
            <div style={{
              background: '#1e293b', borderRadius: 16, padding: 24, marginBottom: 20,
              border: '1px solid #334155', display: 'flex', alignItems: 'center', justifyContent: 'space-between',
            }}>
              <div>
                <div style={{ fontSize: 16, fontWeight: 600, color: '#f1f5f9', marginBottom: 4 }}>Trạng thái nhận cuốc</div>
                <div style={{ fontSize: 13, color: isOnline ? '#10b981' : '#64748b' }}>
                  {isOnline ? 'Đang online — sẵn sàng nhận cuốc' : 'Offline — không nhận cuốc mới'}
                </div>
                {currentPos && (
                  <div style={{ fontSize: 12, color: '#64748b', marginTop: 6 }}>
                    GPS: {currentPos.lat.toFixed(5)}, {currentPos.lng.toFixed(5)} (mỗi 3 giây)
                  </div>
                )}
              </div>
              <button onClick={toggleOnline} disabled={onlineLoading} style={{
                padding: '10px 24px', borderRadius: 10, border: 'none', cursor: 'pointer',
                background: isOnline ? '#ef4444' : '#10b981', color: '#fff',
                fontSize: 14, fontWeight: 700, minWidth: 110, fontFamily: 'inherit',
              }}>
                {onlineLoading ? '...' : isOnline ? 'Offline' : 'Online'}
              </button>
            </div>

            <div style={{ display: 'grid', gridTemplateColumns: 'repeat(3, 1fr)', gap: 16, marginBottom: 20 }}>
              {[
                { label: 'Tổng chuyến', value: trips.length },
                { label: 'Hoàn thành', value: trips.filter(t => t.status === 'Completed').length },
                { label: 'Thông báo mới', value: unreadCount },
              ].map(s => (
                <div key={s.label} style={{
                  background: '#1e293b', borderRadius: 12, padding: 20,
                  border: '1px solid #334155', textAlign: 'center',
                }}>
                  <div style={{ fontSize: 28, fontWeight: 700, color: '#10b981' }}>{s.value}</div>
                  <div style={{ fontSize: 13, color: '#64748b' }}>{s.label}</div>
                </div>
              ))}
            </div>

            <div style={{ display: 'grid', gridTemplateColumns: 'repeat(3, 1fr)', gap: 16, marginBottom: 20 }}>
              <div style={{ background: '#1e293b', borderRadius: 12, padding: 20, border: '1px solid #334155' }}>
                <div style={{ fontSize: 12, color: '#64748b', marginBottom: 8 }}>Doanh thu cuốc hôm nay</div>
                <div style={{ fontSize: 22, fontWeight: 700, color: '#10b981' }}>{fmtCurrency(dailyEarnings?.tripRevenue ?? 0)}</div>
                <div style={{ marginTop: 6, fontSize: 12, color: '#94a3b8' }}>Tiền từ các cuốc đã hoàn thành</div>
              </div>

              <div style={{ background: '#1e293b', borderRadius: 12, padding: 20, border: '1px solid #334155' }}>
                <div style={{ fontSize: 12, color: '#64748b', marginBottom: 8 }}>KPI ngày</div>
                <div style={{ fontSize: 22, fontWeight: 700, color: '#f59e0b' }}>{fmtCurrency(dailyEarnings?.kpiPayout ?? 0)}</div>
                <div style={{ marginTop: 6, fontSize: 12, color: '#94a3b8' }}>
                  {dailyEarnings?.isKpiFinalized
                    ? `Đã chốt • hệ số ${(dailyEarnings.kpiRate * 100).toFixed(0)}%`
                    : 'Chưa chốt KPI cho ngày này'}
                </div>
              </div>

              <div style={{ background: '#1e293b', borderRadius: 12, padding: 20, border: '1px solid #334155' }}>
                <div style={{ fontSize: 12, color: '#64748b', marginBottom: 8 }}>Lợi nhuận ngày</div>
                <div style={{ fontSize: 22, fontWeight: 700, color: '#38bdf8' }}>{fmtCurrency(dailyEarnings?.netProfit ?? 0)}</div>
                <div style={{ marginTop: 6, fontSize: 12, color: '#94a3b8' }}>Doanh thu cuốc + KPI</div>
              </div>
            </div>

            {activeTrip && (
              <div style={{
                background: 'rgba(16,185,129,0.1)', border: '1px solid #10b981',
                borderRadius: 12, padding: 16, display: 'flex', alignItems: 'center', justifyContent: 'space-between',
              }}>
                <div>
                  <div style={{ fontWeight: 600, color: '#10b981', marginBottom: 4 }}>Có chuyến đang chạy</div>
                  <div style={{ fontSize: 13, color: '#94a3b8' }}>{activeTrip.pickupAddress} → {activeTrip.dropoffAddress}</div>
                </div>
                <button onClick={() => setTab('current')} style={{
                  padding: '8px 16px', borderRadius: 8, border: 'none',
                  background: '#10b981', color: '#fff', cursor: 'pointer', fontWeight: 600, fontSize: 13, fontFamily: 'inherit',
                }}>Xem</button>
              </div>
            )}
          </div>
        )}

        {/* Current trip */}
        {tab === 'current' && (
          <div>
            <h2 style={{ margin: '0 0 24px', color: '#f1f5f9' }}>Chuyến hiện tại</h2>
            {!activeTrip ? (
              <div style={{ background: '#1e293b', borderRadius: 16, padding: 40, textAlign: 'center', color: '#64748b' }}>
                <div style={{ fontSize: 40, marginBottom: 12 }}>🚗</div>
                <div>Không có chuyến nào đang chạy.</div>
                <div style={{ fontSize: 13, marginTop: 8 }}>Hãy bật Online để nhận cuốc mới.</div>
              </div>
            ) : (
              <div style={{ background: '#1e293b', borderRadius: 16, padding: 24, border: '1px solid #334155' }}>
                <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start', marginBottom: 20 }}>
                  <h3 style={{ margin: 0, color: '#f1f5f9' }}>Chi tiết chuyến</h3>
                  {tripStatusBadge(activeTrip.status)}
                </div>
                <div style={{ background: '#0f172a', borderRadius: 10, padding: 16, marginBottom: 20 }}>
                  <div style={{ marginBottom: 12 }}>
                    <div style={{ fontSize: 11, color: '#10b981', fontWeight: 600, marginBottom: 4 }}>ĐIỂM ĐÓN</div>
                    <div style={{ color: '#e2e8f0' }}>{activeTrip.pickupAddress}</div>
                  </div>
                  <div style={{ height: 1, background: '#1e293b', margin: '8px 0' }} />
                  <div>
                    <div style={{ fontSize: 11, color: '#f59e0b', fontWeight: 600, marginBottom: 4 }}>ĐIỂM TRẢ</div>
                    <div style={{ color: '#e2e8f0' }}>{activeTrip.dropoffAddress}</div>
                  </div>
                </div>
                {activeTrip.finalFare && (
                  <div style={{ textAlign: 'center', marginBottom: 20 }}>
                    <div style={{ fontSize: 11, color: '#64748b', marginBottom: 4 }}>Cước phí</div>
                    <div style={{ fontSize: 24, fontWeight: 700, color: '#10b981' }}>{fmtCurrency(activeTrip.finalFare)}</div>
                  </div>
                )}

                {['Accepted', 'DriverEnRoute', 'DriverArrived', 'InProgress'].includes(activeTrip.status) && (
                  <div style={{ marginBottom: 16 }}>
                    <TripLiveMap
                      riderPos={customerPos}
                      driverPos={currentPos}
                      pickupPos={{ lat: activeTrip.pickupLatitude, lng: activeTrip.pickupLongitude }}
                      dropoffPos={{ lat: activeTrip.dropoffLatitude, lng: activeTrip.dropoffLongitude }}
                      height={220}
                    />
                  </div>
                )}

                <div style={{ display: 'flex', flexDirection: 'column', gap: 10 }}>
                  {activeTrip.status === 'Accepted' && (
                    <button onClick={() => updateTripStatus('DriverEnRoute')} style={btnStyle('#3b82f6')}>Bắt đầu đến đón khách</button>
                  )}
                  {activeTrip.status === 'DriverEnRoute' && (
                    <button onClick={() => updateTripStatus('DriverArrived')} style={btnStyle('#f59e0b')}>Đã đến điểm đón</button>
                  )}
                  {activeTrip.status === 'DriverArrived' && (
                    <button onClick={() => updateTripStatus('InProgress')} style={btnStyle('#10b981')}>Bắt đầu chuyến đi</button>
                  )}
                  {activeTrip.status === 'InProgress' && (
                    <>
                      <div style={{ background: '#0f172a', border: '1px solid #334155', borderRadius: 10, padding: 12 }}>
                        <label style={{ display: 'block', color: '#94a3b8', fontSize: 12, marginBottom: 8 }}>Quãng đường thực tế (km)</label>
                        <input
                          type="number"
                          min={0.1}
                          step={0.1}
                          value={distanceInput}
                          onChange={e => setDistanceInput(e.target.value)}
                          placeholder="VD: 12.5"
                          style={{
                            width: '100%', padding: '10px 12px', borderRadius: 8,
                            border: '1px solid #334155', background: '#1e293b',
                            color: '#f1f5f9', fontSize: 14,
                          }}
                        />
                        <div style={{ marginTop: 6, fontSize: 11, color: '#64748b' }}>
                          Nếu không chắc, bạn có thể dùng số km ước tính và chỉnh lại cho chính xác.
                        </div>
                      </div>
                      <button onClick={completeTrip} style={btnStyle('#10b981')}>Hoàn thành chuyến</button>
                    </>
                  )}
                </div>

                {['Accepted', 'DriverEnRoute', 'DriverArrived', 'InProgress'].includes(activeTrip.status) && (
                  <div style={{ marginTop: 16 }}>
                    <TripChat
                      tripId={activeTrip.id}
                      currentUserId={user.id}
                      hubRef={hubRef}
                      externalMessages={chatMessages}
                    />
                  </div>
                )}
              </div>
            )}
          </div>
        )}

        {/* History */}
        {tab === 'history' && (
          <div>
            <h2 style={{ margin: '0 0 24px', color: '#f1f5f9' }}>Lịch sử chuyến</h2>
            {trips.length === 0 ? (
              <div style={{ background: '#1e293b', borderRadius: 16, padding: 40, textAlign: 'center', color: '#64748b' }}>Chưa có chuyến nào.</div>
            ) : (
              <div style={{ display: 'flex', flexDirection: 'column', gap: 12 }}>
                {trips.map(t => (
                  <div key={t.id} style={{ background: '#1e293b', borderRadius: 12, padding: 18, border: '1px solid #334155' }}>
                    <div style={{ display: 'flex', justifyContent: 'space-between', marginBottom: 10 }}>
                      {tripStatusBadge(t.status)}
                      <span style={{ fontSize: 12, color: '#64748b' }}>{fmtTime(t.acceptedAt)}</span>
                    </div>
                    <div style={{ fontSize: 13, color: '#94a3b8', marginBottom: 4 }}>
                      <span style={{ color: '#10b981' }}>Đón:</span> {t.pickupAddress}
                    </div>
                    <div style={{ fontSize: 13, color: '#94a3b8', marginBottom: 10 }}>
                      <span style={{ color: '#f59e0b' }}>Trả:</span> {t.dropoffAddress}
                    </div>
                    {t.finalFare && (
                      <div style={{ fontSize: 14, fontWeight: 700, color: '#10b981' }}>
                        {fmtCurrency(t.finalFare)}
                        {t.actualDistanceKm && <span style={{ fontWeight: 400, color: '#64748b', marginLeft: 8 }}>{t.actualDistanceKm} km</span>}
                      </div>
                    )}
                  </div>
                ))}
              </div>
            )}
          </div>
        )}

        {/* Notifications */}
        {tab === 'notifications' && (
          <div>
            <h2 style={{ margin: '0 0 24px', color: '#f1f5f9' }}>Thông báo</h2>
            {notifs.length === 0 ? (
              <div style={{ background: '#1e293b', borderRadius: 16, padding: 40, textAlign: 'center', color: '#64748b' }}>Không có thông báo.</div>
            ) : (
              <div style={{ display: 'flex', flexDirection: 'column', gap: 10 }}>
                {notifs.map(n => (
                  <div key={n.id} style={{
                    background: '#1e293b', borderRadius: 12, padding: 16,
                    border: `1px solid ${n.isRead ? '#334155' : '#10b981'}`, opacity: n.isRead ? 0.7 : 1,
                  }}>
                    <div style={{ fontWeight: 600, color: '#f1f5f9', marginBottom: 4 }}>{n.title}</div>
                    <div style={{ fontSize: 13, color: '#94a3b8', marginBottom: 6 }}>{n.body}</div>
                    <div style={{ fontSize: 11, color: '#64748b' }}>{fmtTime(n.createdAt)}</div>
                  </div>
                ))}
              </div>
            )}
          </div>
        )}

        {/* Profile */}
        {tab === 'profile' && (
          <div style={{ display: 'flex', flexDirection: 'column', gap: 20 }}>
            <h2 style={{ margin: 0, color: '#f1f5f9' }}>Hồ sơ</h2>

            {/* User card */}
            <div style={{ background: '#1e293b', borderRadius: 16, padding: 24, border: '1px solid #334155' }}>
              <div style={{ display: 'flex', alignItems: 'center', gap: 16, marginBottom: 20 }}>
                <div style={{
                  width: 64, height: 64, borderRadius: '50%',
                  background: 'linear-gradient(135deg, #10b981, #059669)',
                  display: 'flex', alignItems: 'center', justifyContent: 'center',
                  fontSize: 26, fontWeight: 700, color: '#fff', flexShrink: 0,
                }}>{displayName[0]?.toUpperCase()}</div>
                <div>
                  <div style={{ fontSize: 20, fontWeight: 700, color: '#f1f5f9' }}>{displayName}</div>
                  <div style={{ fontSize: 13, color: '#64748b', marginTop: 2 }}>{user.email}</div>
                  <div style={{ display: 'flex', gap: 6, marginTop: 8, flexWrap: 'wrap' }}>
                    {user.roles?.map(r => (
                      <span key={r} style={{
                        background: r === 'DRIVER' ? 'rgba(16,185,129,0.15)' : 'rgba(99,102,241,0.15)',
                        color: r === 'DRIVER' ? '#10b981' : '#818cf8',
                        border: `1px solid ${r === 'DRIVER' ? '#10b981' : '#818cf8'}`,
                        padding: '2px 10px', borderRadius: 20, fontSize: 11, fontWeight: 700,
                      }}>{r}</span>
                    ))}
                  </div>
                </div>
              </div>
              {[
                { label: 'Email', value: user.email },
                { label: 'Số điện thoại', value: user.phone ?? '—' },
                { label: 'Xác thực email', value: user.emailVerified ? 'Đã xác thực' : 'Chưa xác thực' },
              ].map(row => (
                <div key={row.label} style={{
                  display: 'flex', justifyContent: 'space-between', alignItems: 'center',
                  padding: '11px 0', borderBottom: '1px solid #0f172a',
                }}>
                  <span style={{ color: '#64748b', fontSize: 14 }}>{row.label}</span>
                  <span style={{ color: '#f1f5f9', fontSize: 14, fontWeight: 500 }}>{row.value}</span>
                </div>
              ))}
            </div>

            {/* Driver info */}
            {profile && (
              <div style={{ background: '#1e293b', borderRadius: 16, padding: 24, border: '1px solid #334155' }}>
                <div style={{ fontSize: 13, fontWeight: 700, color: '#10b981', letterSpacing: 1, marginBottom: 16 }}>THÔNG TIN TÀI XẾ</div>
                <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 12, marginBottom: 16 }}>
                  {[
                    { label: 'Mã tài xế', value: profile.driverCode ?? '—' },
                    { label: 'Trạng thái', value: profile.status === 'Active' ? 'Đang hoạt động' : profile.status === 'Pending' ? 'Chờ duyệt' : 'Tạm ngưng' },
                    { label: 'Đánh giá', value: profile.rating > 0 ? `${profile.rating.toFixed(1)} ⭐` : 'Chưa có' },
                    { label: 'Tổng chuyến', value: String(profile.totalRides) },
                    { label: 'Số bằng lái', value: profile.licenseNumber ?? '—' },
                    { label: 'Hạn bằng lái', value: profile.licenseExpiry ? new Date(profile.licenseExpiry).toLocaleDateString('vi-VN') : '—' },
                  ].map(row => (
                    <div key={row.label} style={{
                      background: '#0f172a', borderRadius: 10, padding: '12px 16px',
                    }}>
                      <div style={{ fontSize: 11, color: '#64748b', marginBottom: 4 }}>{row.label}</div>
                      <div style={{ fontSize: 14, fontWeight: 600, color: '#f1f5f9' }}>{row.value}</div>
                    </div>
                  ))}
                </div>
              </div>
            )}

            {/* Vehicles */}
            {profile && profile.vehicles.length > 0 && (
              <div style={{ background: '#1e293b', borderRadius: 16, padding: 24, border: '1px solid #334155' }}>
                <div style={{ fontSize: 13, fontWeight: 700, color: '#10b981', letterSpacing: 1, marginBottom: 16 }}>
                  XE ({profile.vehicles.length})
                </div>
                <div style={{ display: 'flex', flexDirection: 'column', gap: 12 }}>
                  {profile.vehicles.map((v: VehicleDto) => (
                    (() => {
                      const draft = vehicleDraftById[v.id] ?? {
                        make: v.make,
                        model: v.model,
                        year: v.year,
                        color: v.color,
                        plateNumber: v.plateNumber,
                        seatCount: v.seatCount,
                        imageUrl: v.imageUrl ?? '',
                        vehicleType: normalizeVehicleType(v.vehicleType),
                      }

                      return (
                    <div key={v.id} style={{
                      background: '#0f172a', borderRadius: 12, padding: 16,
                      border: `1px solid ${v.isActive ? '#10b981' : '#334155'}`,
                    }}>
                      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start', marginBottom: 12 }}>
                        <div>
                          <div style={{ fontSize: 16, fontWeight: 700, color: '#f1f5f9' }}>
                            {v.make} {v.model}
                          </div>
                          <div style={{ fontSize: 12, color: '#64748b', marginTop: 2 }}>{v.year} · {v.color}</div>
                        </div>
                        <div style={{ display: 'flex', gap: 6, flexWrap: 'wrap', justifyContent: 'flex-end' }}>
                          <span style={{
                            background: 'rgba(99,102,241,0.15)', color: '#818cf8',
                            border: '1px solid #818cf8', padding: '2px 8px', borderRadius: 20, fontSize: 11, fontWeight: 600,
                          }}>{v.vehicleType === 'ElectricBike' ? 'Xe điện' : `${v.seatCount} chỗ`}</span>
                          {v.isActive && (
                            <span style={{
                              background: 'rgba(16,185,129,0.15)', color: '#10b981',
                              border: '1px solid #10b981', padding: '2px 8px', borderRadius: 20, fontSize: 11, fontWeight: 600,
                            }}>Đang dùng</span>
                          )}
                          {v.isVerified && (
                            <span style={{
                              background: 'rgba(245,158,11,0.15)', color: '#f59e0b',
                              border: '1px solid #f59e0b', padding: '2px 8px', borderRadius: 20, fontSize: 11, fontWeight: 600,
                            }}>Đã xác minh</span>
                          )}
                          {!v.isVerified && (
                            <span style={{
                              background: 'rgba(245,158,11,0.15)', color: '#f59e0b',
                              border: '1px solid #f59e0b', padding: '2px 8px', borderRadius: 20, fontSize: 11, fontWeight: 600,
                            }}>Chờ admin duyệt</span>
                          )}
                        </div>
                      </div>
                      <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 8 }}>
                        {[
                          { label: 'Biển số', value: v.plateNumber },
                          { label: 'Số chỗ', value: `${v.seatCount} chỗ` },
                        ].map(r => (
                          <div key={r.label} style={{ background: '#1e293b', borderRadius: 8, padding: '8px 12px' }}>
                            <div style={{ fontSize: 10, color: '#64748b', marginBottom: 2 }}>{r.label}</div>
                            <div style={{ fontSize: 13, fontWeight: 600, color: '#e2e8f0' }}>{r.value}</div>
                          </div>
                        ))}
                      </div>

                      <div style={{ marginTop: 12, background: '#1e293b', borderRadius: 10, padding: 12, border: '1px solid #334155' }}>
                        <div style={{ fontSize: 12, color: '#94a3b8', marginBottom: 8 }}>Cập nhật thông tin xe (gửi admin duyệt)</div>
                        <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 8 }}>
                          <input
                            value={draft.make}
                            onChange={e => setVehicleDraftField(v.id, 'make', e.target.value)}
                            placeholder="Hãng xe"
                            style={{ padding: '8px 10px', borderRadius: 8, border: '1px solid #334155', background: '#0f172a', color: '#e2e8f0' }}
                          />
                          <input
                            value={draft.model}
                            onChange={e => setVehicleDraftField(v.id, 'model', e.target.value)}
                            placeholder="Dòng xe"
                            style={{ padding: '8px 10px', borderRadius: 8, border: '1px solid #334155', background: '#0f172a', color: '#e2e8f0' }}
                          />
                          <input
                            value={draft.year}
                            onChange={e => setVehicleDraftField(v.id, 'year', Number(e.target.value) || draft.year)}
                            type="number"
                            min={2000}
                            max={2100}
                            placeholder="Năm sản xuất"
                            style={{ padding: '8px 10px', borderRadius: 8, border: '1px solid #334155', background: '#0f172a', color: '#e2e8f0' }}
                          />
                          <input
                            value={draft.color}
                            onChange={e => setVehicleDraftField(v.id, 'color', e.target.value)}
                            placeholder="Màu xe"
                            style={{ padding: '8px 10px', borderRadius: 8, border: '1px solid #334155', background: '#0f172a', color: '#e2e8f0' }}
                          />
                          <input
                            value={draft.plateNumber}
                            onChange={e => setVehicleDraftField(v.id, 'plateNumber', e.target.value.toUpperCase())}
                            placeholder="Biển số"
                            style={{ padding: '8px 10px', borderRadius: 8, border: '1px solid #334155', background: '#0f172a', color: '#e2e8f0' }}
                          />
                          <select
                            value={draft.seatCount}
                            onChange={e => {
                              const seatCount = Number(e.target.value)
                              setVehicleDraftField(v.id, 'seatCount', seatCount)
                              setVehicleDraftField(v.id, 'vehicleType', vehicleTypeBySeatCount(seatCount))
                            }}
                            style={{ padding: '8px 10px', borderRadius: 8, border: '1px solid #334155', background: '#0f172a', color: '#e2e8f0' }}
                          >
                            <option value={1}>1 chỗ</option>
                            <option value={4}>4 chỗ</option>
                            <option value={7}>7 chỗ</option>
                            <option value={9}>9 chỗ</option>
                          </select>
                          <select
                            value={draft.vehicleType}
                            onChange={e => {
                              const vehicleType = e.target.value as VehicleUpdateDraft['vehicleType']
                              setVehicleDraftField(v.id, 'vehicleType', vehicleType)
                              setVehicleDraftField(v.id, 'seatCount', seatCountByVehicleType(vehicleType))
                            }}
                            style={{ padding: '8px 10px', borderRadius: 8, border: '1px solid #334155', background: '#0f172a', color: '#e2e8f0' }}
                          >
                            <option value="ElectricBike">Xe điện</option>
                            <option value="Seat4">4 chỗ</option>
                            <option value="Seat7">7 chỗ</option>
                            <option value="Seat9">9 chỗ</option>
                          </select>
                          <input
                            value={draft.imageUrl}
                            onChange={e => setVehicleDraftField(v.id, 'imageUrl', e.target.value)}
                            placeholder="Link ảnh xe (tuỳ chọn)"
                            style={{ padding: '8px 10px', borderRadius: 8, border: '1px solid #334155', background: '#0f172a', color: '#e2e8f0' }}
                          />
                        </div>
                        <div style={{ marginTop: 10, display: 'flex', gap: 8, alignItems: 'center', flexWrap: 'wrap' }}>
                          <button
                            onClick={() => submitVehicleUpdateRequest(v)}
                            style={{ ...btnStyle('#3b82f6'), width: 'auto', padding: '8px 14px', fontSize: 13 }}
                          >
                            Gửi yêu cầu cập nhật xe
                          </button>
                        </div>
                        <div style={{ marginTop: 6, fontSize: 11, color: '#64748b' }}>
                          Sau khi gửi, xe sẽ chuyển sang trạng thái chờ admin duyệt lại.
                        </div>
                      </div>
                    </div>
                      )
                    })()
                  ))}
                </div>
              </div>
            )}
          </div>
        )}
      </main>
    </div>
  )
}

// ─── Root Component ───────────────────────────────────────────────────────────

export default function DriverDashboardPage({ user, onLogout }: Props) {
  const nav = useNavigate()
  const [pageState, setPageState] = useState<PageState>('loading')
  const [driverProfile, setDriverProfile] = useState<DriverDto | null>(null)

  useEffect(() => {
    driversApi.me()
      .then(profile => {
        setDriverProfile(profile)
        if (!profile) setPageState('no-profile')
        else if (profile.status === 'Active') setPageState('active')
        else setPageState('pending')
      })
      .catch(() => setPageState('no-profile'))
  }, [])

  const handleLogout = async () => {
    try { await auth.logout() } catch { /* ignore */ }
    onLogout()
    nav('/login', { replace: true })
  }

  const handleOnboardingComplete = (profile: DriverDto) => {
    setDriverProfile(profile)
    setPageState('pending')
  }

  if (pageState === 'loading') {
    return (
      <div style={{ minHeight: '100vh', background: '#0f172a', display: 'flex', alignItems: 'center', justifyContent: 'center' }}>
        <div style={{ textAlign: 'center', color: '#64748b' }}>
          <div style={{ fontSize: 32, marginBottom: 12 }}>🚗</div>
          <div>Đang tải...</div>
        </div>
      </div>
    )
  }

  if (pageState === 'no-profile') {
    return <OnboardingWizard user={user} onComplete={handleOnboardingComplete} onLogout={handleLogout} />
  }

  if (pageState === 'pending') {
    return <PendingScreen user={user} profile={driverProfile!} onLogout={handleLogout} />
  }

  return <ActiveDashboard user={user} onLogout={onLogout} driverProfile={driverProfile} />
}
