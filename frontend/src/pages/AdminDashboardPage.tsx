import { useState, useEffect, useCallback } from 'react'
import { useNavigate } from 'react-router-dom'
import { adminApi, auth } from '../api'
import AdminDriversLiveMap from '../components/AdminDriversLiveMap'
import type {
  AdminStatsDto, AdminDriverDto, PricingRuleDto, UserDto, TripDto,
} from '../types'

// ─── Helpers ─────────────────────────────────────────────────────────────────

function fmtTime(iso: string) {
  return new Date(iso).toLocaleString('vi-VN', {
    day: '2-digit', month: '2-digit', year: 'numeric',
    hour: '2-digit', minute: '2-digit',
  })
}

function fmtDate(iso: string) {
  return new Date(iso).toLocaleDateString('vi-VN', {
    day: '2-digit', month: '2-digit', year: 'numeric',
  })
}

function fmtCurrency(n?: number | null) {
  if (n == null) return '—'
  return new Intl.NumberFormat('vi-VN', { style: 'currency', currency: 'VND' }).format(n * 23000)
}

function driverStatusBadge(status: string) {
  const cls = status === 'Active' ? 'badge-success'
    : status === 'Pending' ? 'badge-warning'
    : 'badge-danger'
  const label = status === 'Active' ? 'Hoạt động'
    : status === 'Pending' ? 'Chờ duyệt'
    : 'Tạm khoá'
  return <span className={`badge ${cls}`}>{label}</span>
}

function userStatusBadge(status: string) {
  const cls = status === 'Active' ? 'badge-success'
    : status === 'PendingVerification' ? 'badge-warning'
    : 'badge-danger'
  const label = status === 'Active' ? 'Hoạt động'
    : status === 'PendingVerification' ? 'Chờ xác thực'
    : 'Tạm khoá'
  return <span className={`badge ${cls}`}>{label}</span>
}

function tripStatusBadge(status: string) {
  const map: Record<string, string> = {
    Completed: 'badge-success', Cancelled: 'badge-danger',
    InProgress: 'badge-info', Accepted: 'badge-info',
    DriverEnRoute: 'badge-info', DriverArrived: 'badge-warning',
  }
  const labels: Record<string, string> = {
    Completed: 'Hoàn thành', Cancelled: 'Đã huỷ',
    InProgress: 'Đang đi', Accepted: 'Đã nhận',
    DriverEnRoute: 'Tài xế đến', DriverArrived: 'Đã đến',
  }
  return <span className={`badge ${map[status] ?? 'badge-neutral'}`}>{labels[status] ?? status}</span>
}

const VEHICLE_LABELS: Record<string, string> = {
  ElectricBike: 'Xe điện', Seat4: '4 chỗ', Seat7: '7 chỗ', Seat9: '9 chỗ',
}

// ─── Types ────────────────────────────────────────────────────────────────────

type Tab = 'overview' | 'applications' | 'drivers' | 'map' | 'users' | 'pricing' | 'trips'

interface Props { user: UserDto; onLogout: () => void }

// ─── Pricing Editor Row ───────────────────────────────────────────────────────

interface PricingRowProps {
  rule: PricingRuleDto
  onSave: (id: string, data: Omit<PricingRuleDto, 'id' | 'vehicleType' | 'isActive'>) => Promise<void>
}

function PricingRow({ rule, onSave }: PricingRowProps) {
  const [editing, setEditing] = useState(false)
  const [saving, setSaving] = useState(false)
  const [form, setForm] = useState({
    baseFare: rule.baseFare,
    perKmRate: rule.perKmRate,
    perMinuteRate: rule.perMinuteRate,
    minimumFare: rule.minimumFare,
    surgeMultiplier: rule.surgeMultiplier,
  })

  const set = (k: keyof typeof form) => (e: React.ChangeEvent<HTMLInputElement>) =>
    setForm(f => ({ ...f, [k]: parseFloat(e.target.value) || 0 }))

  const save = async () => {
    setSaving(true)
    try {
      await onSave(rule.id, form)
      setEditing(false)
    } finally {
      setSaving(false)
    }
  }

  const vehicleLabel = VEHICLE_LABELS[rule.vehicleType] ?? rule.vehicleType

  if (!editing) {
    return (
      <tr>
        <td style={{ fontWeight: 600 }}>{vehicleLabel}</td>
        <td>${rule.baseFare.toFixed(2)}</td>
        <td>${rule.perKmRate.toFixed(2)}/km</td>
        <td>${rule.perMinuteRate.toFixed(2)}/min</td>
        <td>${rule.minimumFare.toFixed(2)}</td>
        <td>×{rule.surgeMultiplier.toFixed(1)}</td>
        <td>
          <button className="btn btn-secondary btn-sm" onClick={() => setEditing(true)}>
            Chỉnh sửa
          </button>
        </td>
      </tr>
    )
  }

  return (
    <tr style={{ background: 'var(--primary-light)' }}>
      <td style={{ fontWeight: 600 }}>{vehicleLabel}</td>
      <td><input type="number" step="0.01" value={form.baseFare} onChange={set('baseFare')} style={{ width: 80 }} /></td>
      <td><input type="number" step="0.01" value={form.perKmRate} onChange={set('perKmRate')} style={{ width: 80 }} /></td>
      <td><input type="number" step="0.001" value={form.perMinuteRate} onChange={set('perMinuteRate')} style={{ width: 80 }} /></td>
      <td><input type="number" step="0.01" value={form.minimumFare} onChange={set('minimumFare')} style={{ width: 80 }} /></td>
      <td><input type="number" step="0.1" min="1" value={form.surgeMultiplier} onChange={set('surgeMultiplier')} style={{ width: 70 }} /></td>
      <td style={{ display: 'flex', gap: 8 }}>
        <button className="btn btn-primary btn-sm" onClick={save} disabled={saving}>
          {saving ? <span className="spinner" /> : 'Lưu'}
        </button>
        <button className="btn btn-ghost btn-sm" onClick={() => setEditing(false)}>Huỷ</button>
      </td>
    </tr>
  )
}

// ─── Main Component ───────────────────────────────────────────────────────────

export default function AdminDashboardPage({ user, onLogout }: Props) {
  const nav = useNavigate()
  const [tab, setTab] = useState<Tab>('overview')
  const [toast, setToast] = useState<{ msg: string; type: 'success' | 'error' } | null>(null)

  // Data states
  const [stats, setStats] = useState<AdminStatsDto | null>(null)
  const [applications, setApplications] = useState<AdminDriverDto[]>([])
  const [appTotal, setAppTotal] = useState(0)
  const [drivers, setDrivers] = useState<AdminDriverDto[]>([])
  const [driversTotal, setDriversTotal] = useState(0)
  const [mapDrivers, setMapDrivers] = useState<AdminDriverDto[]>([])
  const [users, setUsers] = useState<UserDto[]>([])
  const [usersTotal, setUsersTotal] = useState(0)
  const [pricing, setPricing] = useState<PricingRuleDto[]>([])
  const [trips, setTrips] = useState<TripDto[]>([])
  const [tripsTotal, setTripsTotal] = useState(0)

  const [loading, setLoading] = useState(false)
  const [page, setPage] = useState(1)
  const PAGE_SIZE = 20

  const showToast = (msg: string, type: 'success' | 'error' = 'success') => {
    setToast({ msg, type })
    setTimeout(() => setToast(null), 3000)
  }

  // Load stats
  const loadStats = useCallback(async () => {
    try {
      const s = await adminApi.stats()
      setStats(s)
    } catch { /* ignore */ }
  }, [])

  // Load data per tab
  const loadTab = useCallback(async (t: Tab, p = 1) => {
    setLoading(true)
    try {
      if (t === 'overview') {
        await loadStats()
      } else if (t === 'applications') {
        const res = await adminApi.drivers(p, PAGE_SIZE, 'Pending')
        setApplications(res.items)
        setAppTotal(res.total)
      } else if (t === 'drivers') {
        const res = await adminApi.drivers(p, PAGE_SIZE)
        setDrivers(res.items)
        setDriversTotal(res.total)
      } else if (t === 'map') {
        const res = await adminApi.drivers(1, 500, 'Active')
        setMapDrivers(res.items)
      } else if (t === 'users') {
        const res = await adminApi.users(p, PAGE_SIZE)
        setUsers(res.items)
        setUsersTotal(res.total)
      } else if (t === 'pricing') {
        const res = await adminApi.pricing()
        setPricing(res)
      } else if (t === 'trips') {
        const res = await adminApi.trips(p, PAGE_SIZE)
        setTrips(res.items)
        setTripsTotal(res.total)
      }
    } catch (err) {
      showToast(err instanceof Error ? err.message : 'Lỗi tải dữ liệu', 'error')
    } finally {
      setLoading(false)
    }
  }, [loadStats])

  useEffect(() => {
    setPage(1)
    loadTab(tab, 1)
  }, [tab, loadTab])

  useEffect(() => {
    if (page > 1) loadTab(tab, page)
  }, [page, tab, loadTab])

  useEffect(() => {
    if (tab !== 'map') return
    const timer = window.setInterval(() => {
      loadTab('map', 1)
    }, 10000)
    return () => window.clearInterval(timer)
  }, [tab, loadTab])

  // Initial stats load
  useEffect(() => { loadStats() }, [loadStats])

  const handleApprove = async (id: string) => {
    try {
      await adminApi.approveDriver(id)
      showToast('Đã duyệt tài xế thành công')
      loadTab('applications', page)
      loadStats()
    } catch (err) {
      showToast(err instanceof Error ? err.message : 'Lỗi duyệt tài xế', 'error')
    }
  }

  const handleReject = async (id: string) => {
    if (!confirm('Từ chối hồ sơ tài xế này?')) return
    try {
      await adminApi.rejectDriver(id)
      showToast('Đã từ chối hồ sơ')
      loadTab('applications', page)
      loadStats()
    } catch (err) {
      showToast(err instanceof Error ? err.message : 'Lỗi', 'error')
    }
  }

  const handleSuspendDriver = async (id: string) => {
    if (!confirm('Tạm khoá tài xế này?')) return
    try {
      await adminApi.suspendDriver(id)
      showToast('Đã tạm khoá tài xế')
      loadTab('drivers', page)
    } catch (err) {
      showToast(err instanceof Error ? err.message : 'Lỗi', 'error')
    }
  }

  const handleApproveVehicle = async (vehicleId: string) => {
    try {
      await adminApi.approveVehicle(vehicleId)
      showToast('Đã duyệt cập nhật xe')
      loadTab('drivers', page)
    } catch (err) {
      showToast(err instanceof Error ? err.message : 'Lỗi duyệt xe', 'error')
    }
  }

  const handleSetUserStatus = async (id: string, current: string) => {
    const next = current === 'Active' ? 'Suspended' : 'Active'
    const label = next === 'Active' ? 'kích hoạt' : 'tạm khoá'
    if (!confirm(`${label.charAt(0).toUpperCase() + label.slice(1)} tài khoản này?`)) return
    try {
      await adminApi.setUserStatus(id, next)
      showToast(`Đã ${label} tài khoản`)
      loadTab('users', page)
    } catch (err) {
      showToast(err instanceof Error ? err.message : 'Lỗi', 'error')
    }
  }

  const handleSavePricing = async (id: string, data: Omit<PricingRuleDto, 'id' | 'vehicleType' | 'isActive'>) => {
    const updated = await adminApi.updatePricing(id, data)
    setPricing(prev => prev.map(r => r.id === id ? updated : r))
    showToast('Đã cập nhật bảng giá')
  }

  const handleLogout = async () => {
    try { await auth.logout() } catch { /* ignore */ }
    onLogout()
    nav('/login', { replace: true })
  }

  const initials = `${user.firstName?.[0] ?? ''}${user.lastName?.[0] ?? ''}`.toUpperCase() || 'A'

  // ─── Sidebar nav items ────────────────────────────────────────────────────
  const navItems: { id: Tab; icon: string; label: string; badge?: number }[] = [
    { id: 'overview',     icon: '📊', label: 'Tổng quan' },
    { id: 'applications', icon: '📋', label: 'Đơn xin duyệt', badge: stats?.pendingDrivers },
    { id: 'drivers',      icon: '🚗', label: 'Tài xế' },
    { id: 'map',          icon: '🛰️', label: 'Bản đồ tài xế' },
    { id: 'users',        icon: '👥', label: 'Người dùng' },
    { id: 'pricing',      icon: '💰', label: 'Bảng giá' },
    { id: 'trips',        icon: '🗺️', label: 'Chuyến đi' },
  ]

  // ─── Pagination helper ────────────────────────────────────────────────────
  const total = tab === 'applications' ? appTotal
    : tab === 'drivers' ? driversTotal
    : tab === 'users' ? usersTotal
    : tripsTotal
  const totalPages = Math.max(1, Math.ceil(total / PAGE_SIZE))

  const Pagination = () => (
    <div style={{ display: 'flex', alignItems: 'center', gap: 8, marginTop: 16, justifyContent: 'flex-end' }}>
      <span style={{ fontSize: 13, color: 'var(--muted)' }}>
        {(page - 1) * PAGE_SIZE + 1}–{Math.min(page * PAGE_SIZE, total)} / {total}
      </span>
      <button className="btn btn-ghost btn-sm" disabled={page <= 1} onClick={() => setPage(p => p - 1)}>← Trước</button>
      <button className="btn btn-ghost btn-sm" disabled={page >= totalPages} onClick={() => setPage(p => p + 1)}>Sau →</button>
    </div>
  )

  return (
    <div className="dash-shell">
      {/* Sidebar */}
      <aside className="sidebar">
        <div className="sidebar-logo">
          <div className="sidebar-logo-icon">G</div>
          <span className="sidebar-logo-name">Gokt Admin</span>
        </div>

        <nav className="sidebar-nav">
          <div className="sidebar-section-label">Quản lý</div>
          {navItems.map(item => (
            <button
              key={item.id}
              className={`nav-item${tab === item.id ? ' active' : ''}`}
              onClick={() => setTab(item.id)}
            >
              <span className="nav-icon">{item.icon}</span>
              {item.label}
              {item.badge != null && item.badge > 0 && (
                <span className="nav-badge">{item.badge}</span>
              )}
            </button>
          ))}
        </nav>

        <div className="sidebar-footer">
          <div className="sidebar-user">
            <div className="sidebar-avatar">{initials}</div>
            <div className="sidebar-user-info">
              <div className="sidebar-user-name">{user.firstName} {user.lastName}</div>
              <div className="sidebar-user-email">{user.email}</div>
            </div>
          </div>
          <button className="nav-item" onClick={handleLogout} style={{ color: '#f87171' }}>
            <span className="nav-icon">🚪</span>
            Đăng xuất
          </button>
        </div>
      </aside>

      {/* Main */}
      <main className="dash-main">
        <div className="dash-topbar">
          <h1 className="dash-topbar-title">
            {navItems.find(n => n.id === tab)?.label ?? 'Admin'}
          </h1>
          <div style={{ display: 'flex', gap: 8, alignItems: 'center' }}>
            {loading && <span className="spinner" style={{ width: 20, height: 20 }} />}
            <button className="btn btn-ghost btn-sm" onClick={() => loadTab(tab, page)}>
              Làm mới
            </button>
          </div>
        </div>

        <div className="dash-content">
          {/* Toast */}
          {toast && (
            <div className={`alert alert-${toast.type === 'error' ? 'error' : 'success'}`} style={{ marginBottom: 20 }}>
              <span className="alert-icon">{toast.type === 'error' ? '⚠️' : '✅'}</span>
              {toast.msg}
            </div>
          )}

          {/* ── Overview ── */}
          {tab === 'overview' && (
            <div>
              <div className="stats-grid" style={{ gridTemplateColumns: 'repeat(auto-fit, minmax(160px, 1fr))' }}>
                <div className="stat-card">
                  <div className="stat-icon">👥</div>
                  <div className="stat-label">Người dùng</div>
                  <div className="stat-value">{stats?.totalUsers ?? '—'}</div>
                </div>
                <div className="stat-card">
                  <div className="stat-icon">🚗</div>
                  <div className="stat-label">Tổng tài xế</div>
                  <div className="stat-value">{stats?.totalDrivers ?? '—'}</div>
                </div>
                <div className="stat-card" style={{ borderLeft: '4px solid var(--warning)' }}>
                  <div className="stat-icon">📋</div>
                  <div className="stat-label">Chờ duyệt</div>
                  <div className="stat-value" style={{ color: 'var(--warning)' }}>{stats?.pendingDrivers ?? '—'}</div>
                </div>
                <div className="stat-card" style={{ borderLeft: '4px solid var(--success)' }}>
                  <div className="stat-icon">✅</div>
                  <div className="stat-label">Tài xế active</div>
                  <div className="stat-value" style={{ color: 'var(--success)' }}>{stats?.activeDrivers ?? '—'}</div>
                </div>
                <div className="stat-card">
                  <div className="stat-icon">🗺️</div>
                  <div className="stat-label">Tổng chuyến</div>
                  <div className="stat-value">{stats?.totalTrips ?? '—'}</div>
                </div>
              </div>

              {stats?.pendingDrivers != null && stats.pendingDrivers > 0 && (
                <div className="alert alert-warning">
                  <span className="alert-icon">⚠️</span>
                  Có <strong>{stats.pendingDrivers}</strong> tài xế đang chờ duyệt.{' '}
                  <button
                    onClick={() => setTab('applications')}
                    style={{ background: 'none', border: 'none', color: '#b45309', fontWeight: 700, cursor: 'pointer', padding: 0, textDecoration: 'underline' }}
                  >
                    Xem ngay
                  </button>
                </div>
              )}
            </div>
          )}

          {/* ── Driver Applications ── */}
          {tab === 'applications' && (
            <div className="card">
              <div className="card-header" style={{ paddingBottom: 16 }}>
                <span className="card-title">Hồ sơ tài xế chờ duyệt</span>
                <span className="badge badge-warning">{appTotal} hồ sơ</span>
              </div>
              <div className="card-body" style={{ padding: 0 }}>
                {applications.length === 0 && !loading ? (
                  <div className="empty-state">
                    <div className="empty-icon">✅</div>
                    <div className="empty-title">Không có hồ sơ nào chờ duyệt</div>
                  </div>
                ) : (
                  <table style={{ width: '100%', borderCollapse: 'collapse', fontSize: 14 }}>
                    <thead>
                      <tr style={{ borderBottom: '2px solid var(--border)', background: '#f8fafc' }}>
                        <th style={th}>Tài xế</th>
                        <th style={th}>Bằng lái</th>
                        <th style={th}>Xe</th>
                        <th style={th}>Ngày đăng ký</th>
                        <th style={th}>Hành động</th>
                      </tr>
                    </thead>
                    <tbody>
                      {applications.map(d => (
                        <tr key={d.id} style={{ borderBottom: '1px solid var(--border)' }}>
                          <td style={td}>
                            <div style={{ fontWeight: 600 }}>{d.fullName}</div>
                            <div style={{ fontSize: 12, color: 'var(--muted)' }}>{d.email}</div>
                            <div style={{ fontSize: 12, color: 'var(--muted)' }}>{d.phone}</div>
                          </td>
                          <td style={td}>
                            <div style={{ fontFamily: 'var(--mono)', fontSize: 13 }}>{d.licenseNumber}</div>
                            <div style={{ fontSize: 12, color: 'var(--muted)' }}>HH: {fmtDate(d.licenseExpiry)}</div>
                          </td>
                          <td style={td}>
                            {d.vehicles.length === 0 ? (
                              <span style={{ color: 'var(--muted)', fontSize: 12 }}>Chưa có xe</span>
                            ) : d.vehicles.map(v => (
                              <div key={v.id} style={{ fontSize: 13 }}>
                                {v.make} {v.model} — {v.plateNumber}
                                <span className="badge badge-neutral" style={{ marginLeft: 6, fontSize: 10 }}>
                                  {VEHICLE_LABELS[v.vehicleType] ?? v.vehicleType}
                                </span>
                              </div>
                            ))}
                          </td>
                          <td style={td}>{fmtDate(d.createdAt)}</td>
                          <td style={td}>
                            <div style={{ display: 'flex', gap: 8 }}>
                              <button className="btn btn-primary btn-sm" onClick={() => handleApprove(d.id)}>
                                Duyệt
                              </button>
                              <button className="btn btn-danger btn-sm" onClick={() => handleReject(d.id)}>
                                Từ chối
                              </button>
                            </div>
                          </td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                )}
              </div>
              {appTotal > PAGE_SIZE && (
                <div className="card-footer"><Pagination /></div>
              )}
            </div>
          )}

          {/* ── All Drivers ── */}
          {tab === 'drivers' && (
            <div className="card">
              <div className="card-header" style={{ paddingBottom: 16 }}>
                <span className="card-title">Tất cả tài xế</span>
                <span className="badge badge-neutral">{driversTotal} tài xế</span>
              </div>
              <div className="card-body" style={{ padding: 0 }}>
                {drivers.length === 0 && !loading ? (
                  <div className="empty-state">
                    <div className="empty-icon">🚗</div>
                    <div className="empty-title">Chưa có tài xế nào</div>
                  </div>
                ) : (
                  <table style={{ width: '100%', borderCollapse: 'collapse', fontSize: 14 }}>
                    <thead>
                      <tr style={{ borderBottom: '2px solid var(--border)', background: '#f8fafc' }}>
                        <th style={th}>Tài xế</th>
                        <th style={th}>Mã</th>
                        <th style={th}>Trạng thái</th>
                        <th style={th}>Đánh giá</th>
                        <th style={th}>Chuyến</th>
                        <th style={th}>Online</th>
                        <th style={th}>Xe chờ duyệt</th>
                        <th style={th}>Hành động</th>
                      </tr>
                    </thead>
                    <tbody>
                      {drivers.map(d => (
                        <tr key={d.id} style={{ borderBottom: '1px solid var(--border)' }}>
                          <td style={td}>
                            <div style={{ fontWeight: 600 }}>{d.fullName}</div>
                            <div style={{ fontSize: 12, color: 'var(--muted)' }}>{d.email}</div>
                          </td>
                          <td style={td}>
                            <code style={{ fontSize: 12, background: '#f1f5f9', padding: '2px 6px', borderRadius: 4 }}>
                              {d.driverCode}
                            </code>
                          </td>
                          <td style={td}>{driverStatusBadge(d.status)}</td>
                          <td style={td}>
                            <span style={{ fontWeight: 700 }}>⭐ {d.rating.toFixed(1)}</span>
                          </td>
                          <td style={td}>{d.totalRides}</td>
                          <td style={td}>
                            <span className={`badge ${d.isOnline ? 'badge-success' : 'badge-neutral'}`}>
                              {d.isOnline ? 'Online' : 'Offline'}
                            </span>
                          </td>
                          <td style={td}>
                            {d.vehicles.filter(v => !v.isVerified).length === 0 ? (
                              <span style={{ color: 'var(--muted)', fontSize: 12 }}>Không có</span>
                            ) : (
                              <div style={{ display: 'flex', flexDirection: 'column', gap: 6 }}>
                                {d.vehicles.filter(v => !v.isVerified).map(v => (
                                  <div key={v.id} style={{ display: 'flex', gap: 6, alignItems: 'center' }}>
                                    <span style={{ fontSize: 12, color: '#b45309' }}>{v.make} {v.model}</span>
                                    <button className="btn btn-primary btn-sm" onClick={() => handleApproveVehicle(v.id)}>
                                      Duyệt xe
                                    </button>
                                  </div>
                                ))}
                              </div>
                            )}
                          </td>
                          <td style={td}>
                            {d.status === 'Active' && (
                              <button className="btn btn-danger btn-sm" onClick={() => handleSuspendDriver(d.id)}>
                                Khoá
                              </button>
                            )}
                            {d.status === 'Suspended' && (
                              <button className="btn btn-secondary btn-sm" onClick={async () => {
                                try {
                                  await adminApi.approveDriver(d.id)
                                  showToast('Đã kích hoạt lại tài xế')
                                  loadTab('drivers', page)
                                } catch (err) {
                                  showToast(err instanceof Error ? err.message : 'Lỗi', 'error')
                                }
                              }}>
                                Kích hoạt
                              </button>
                            )}
                          </td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                )}
              </div>
              {driversTotal > PAGE_SIZE && (
                <div className="card-footer"><Pagination /></div>
              )}
            </div>
          )}

          {/* ── Users ── */}
          {tab === 'map' && (
            <div className="card">
              <div className="card-header" style={{ paddingBottom: 16 }}>
                <span className="card-title">Bản đồ tài xế đang hoạt động</span>
                <span className="badge badge-info">{mapDrivers.filter(d => d.isOnline).length} online</span>
              </div>
              <div className="card-body" style={{ display: 'grid', gap: 14 }}>
                <div style={{ display: 'flex', gap: 16, flexWrap: 'wrap', fontSize: 13, color: 'var(--muted)' }}>
                  <span><strong style={{ color: '#10b981' }}>●</strong> Rảnh</span>
                  <span><strong style={{ color: '#f59e0b' }}>●</strong> Đang bận</span>
                  <span>Cập nhật tự động mỗi 10 giây</span>
                </div>

                {mapDrivers.filter(d => d.latitude != null && d.longitude != null).length === 0 && !loading ? (
                  <div className="empty-state">
                    <div className="empty-icon">📡</div>
                    <div className="empty-title">Chưa có dữ liệu vị trí tài xế</div>
                    <div className="empty-desc">Tài xế cần online và gửi GPS để hiển thị trên bản đồ.</div>
                  </div>
                ) : (
                  <AdminDriversLiveMap drivers={mapDrivers.filter(d => d.isOnline)} />
                )}
              </div>
            </div>
          )}

          {/* ── Users ── */}
          {tab === 'users' && (
            <div className="card">
              <div className="card-header" style={{ paddingBottom: 16 }}>
                <span className="card-title">Tất cả người dùng</span>
                <span className="badge badge-neutral">{usersTotal} người</span>
              </div>
              <div className="card-body" style={{ padding: 0 }}>
                {users.length === 0 && !loading ? (
                  <div className="empty-state">
                    <div className="empty-icon">👥</div>
                    <div className="empty-title">Chưa có người dùng nào</div>
                  </div>
                ) : (
                  <table style={{ width: '100%', borderCollapse: 'collapse', fontSize: 14 }}>
                    <thead>
                      <tr style={{ borderBottom: '2px solid var(--border)', background: '#f8fafc' }}>
                        <th style={th}>Người dùng</th>
                        <th style={th}>Vai trò</th>
                        <th style={th}>Trạng thái</th>
                        <th style={th}>Ngày tạo</th>
                        <th style={th}>Hành động</th>
                      </tr>
                    </thead>
                    <tbody>
                      {users.map(u => (
                        <tr key={u.id} style={{ borderBottom: '1px solid var(--border)' }}>
                          <td style={td}>
                            <div style={{ fontWeight: 600 }}>{u.firstName} {u.lastName}</div>
                            <div style={{ fontSize: 12, color: 'var(--muted)' }}>{u.email}</div>
                            {u.phone && <div style={{ fontSize: 12, color: 'var(--muted)' }}>{u.phone}</div>}
                          </td>
                          <td style={td}>
                            <div style={{ display: 'flex', gap: 4, flexWrap: 'wrap' }}>
                              {u.roles.map(r => (
                                <span key={r} className={`badge ${r === 'ADMIN' ? 'badge-danger' : r === 'DRIVER' ? 'badge-info' : 'badge-neutral'}`}>
                                  {r}
                                </span>
                              ))}
                            </div>
                          </td>
                          <td style={td}>{userStatusBadge(u.status)}</td>
                          <td style={td}>{fmtTime(u.createdAt)}</td>
                          <td style={td}>
                            {!u.roles.includes('ADMIN') && (
                              <button
                                className={`btn btn-sm ${u.status === 'Active' ? 'btn-danger' : 'btn-secondary'}`}
                                onClick={() => handleSetUserStatus(u.id, u.status)}
                              >
                                {u.status === 'Active' ? 'Khoá' : 'Kích hoạt'}
                              </button>
                            )}
                          </td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                )}
              </div>
              {usersTotal > PAGE_SIZE && (
                <div className="card-footer"><Pagination /></div>
              )}
            </div>
          )}

          {/* ── Pricing ── */}
          {tab === 'pricing' && (
            <div className="card">
              <div className="card-header" style={{ paddingBottom: 16 }}>
                <span className="card-title">Bảng giá</span>
                <span style={{ fontSize: 13, color: 'var(--muted)' }}>Giá tính bằng USD</span>
              </div>
              <div className="card-body" style={{ padding: 0 }}>
                {pricing.length === 0 && !loading ? (
                  <div className="empty-state">
                    <div className="empty-icon">💰</div>
                    <div className="empty-title">Không có dữ liệu giá</div>
                  </div>
                ) : (
                  <table style={{ width: '100%', borderCollapse: 'collapse', fontSize: 14 }}>
                    <thead>
                      <tr style={{ borderBottom: '2px solid var(--border)', background: '#f8fafc' }}>
                        <th style={th}>Loại xe</th>
                        <th style={th}>Giá cơ bản</th>
                        <th style={th}>Giá/km</th>
                        <th style={th}>Giá/phút</th>
                        <th style={th}>Giá tối thiểu</th>
                        <th style={th}>Hệ số surge</th>
                        <th style={th}></th>
                      </tr>
                    </thead>
                    <tbody>
                      {pricing.map(r => (
                        <PricingRow key={r.id} rule={r} onSave={handleSavePricing} />
                      ))}
                    </tbody>
                  </table>
                )}
              </div>
            </div>
          )}

          {/* ── Trips ── */}
          {tab === 'trips' && (
            <div className="card">
              <div className="card-header" style={{ paddingBottom: 16 }}>
                <span className="card-title">Tất cả chuyến đi</span>
                <span className="badge badge-neutral">{tripsTotal} chuyến</span>
              </div>
              <div className="card-body" style={{ padding: 0 }}>
                {trips.length === 0 && !loading ? (
                  <div className="empty-state">
                    <div className="empty-icon">🗺️</div>
                    <div className="empty-title">Chưa có chuyến đi nào</div>
                  </div>
                ) : (
                  <table style={{ width: '100%', borderCollapse: 'collapse', fontSize: 14 }}>
                    <thead>
                      <tr style={{ borderBottom: '2px solid var(--border)', background: '#f8fafc' }}>
                        <th style={th}>Lộ trình</th>
                        <th style={th}>Tài xế</th>
                        <th style={th}>Trạng thái</th>
                        <th style={th}>Giá</th>
                        <th style={th}>Thời gian</th>
                      </tr>
                    </thead>
                    <tbody>
                      {trips.map(t => (
                        <tr key={t.id} style={{ borderBottom: '1px solid var(--border)' }}>
                          <td style={td}>
                            <div style={{ fontSize: 13 }}>
                              <span style={{ color: 'var(--success)', fontWeight: 600 }}>↑</span>{' '}
                              {t.pickupAddress.length > 40 ? t.pickupAddress.slice(0, 40) + '…' : t.pickupAddress}
                            </div>
                            <div style={{ fontSize: 13, marginTop: 2 }}>
                              <span style={{ color: 'var(--danger)', fontWeight: 600 }}>↓</span>{' '}
                              {t.dropoffAddress.length > 40 ? t.dropoffAddress.slice(0, 40) + '…' : t.dropoffAddress}
                            </div>
                          </td>
                          <td style={td}>
                            <div style={{ fontWeight: 600 }}>{t.driverName ?? '—'}</div>
                            {t.vehiclePlateNumber && (
                              <code style={{ fontSize: 12, background: '#f1f5f9', padding: '1px 5px', borderRadius: 4 }}>
                                {t.vehiclePlateNumber}
                              </code>
                            )}
                          </td>
                          <td style={td}>{tripStatusBadge(t.status)}</td>
                          <td style={{ ...td, fontWeight: 700 }}>{fmtCurrency(t.finalFare)}</td>
                          <td style={td}>
                            <div style={{ fontSize: 12 }}>{fmtTime(t.acceptedAt)}</div>
                            {t.completedAt && (
                              <div style={{ fontSize: 11, color: 'var(--muted)' }}>
                                → {fmtTime(t.completedAt)}
                              </div>
                            )}
                          </td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                )}
              </div>
              {tripsTotal > PAGE_SIZE && (
                <div className="card-footer"><Pagination /></div>
              )}
            </div>
          )}
        </div>
      </main>
    </div>
  )
}

// ─── Table cell styles ────────────────────────────────────────────────────────

const th: React.CSSProperties = {
  padding: '10px 16px',
  textAlign: 'left',
  fontSize: 12,
  fontWeight: 700,
  color: 'var(--muted)',
  textTransform: 'uppercase',
  letterSpacing: '.06em',
  whiteSpace: 'nowrap',
}

const td: React.CSSProperties = {
  padding: '12px 16px',
  verticalAlign: 'middle',
}
