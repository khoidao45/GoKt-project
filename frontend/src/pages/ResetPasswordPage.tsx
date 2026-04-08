import { useState, type FormEvent } from 'react'
import { Link, useNavigate, useSearchParams } from 'react-router-dom'
import { auth } from '../api'

function checkPwd(p: string) {
  return {
    length:  p.length >= 8,
    upper:   /[A-Z]/.test(p),
    lower:   /[a-z]/.test(p),
    digit:   /\d/.test(p),
    special: /[^A-Za-z0-9]/.test(p),
  }
}

export default function ResetPasswordPage() {
  const nav = useNavigate()
  const [params] = useSearchParams()
  const token = params.get('token') ?? ''

  const [password, setPassword] = useState('')
  const [confirm, setConfirm] = useState('')
  const [showPwd, setShowPwd] = useState(false)
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState('')
  const [done, setDone] = useState(false)

  const rules = checkPwd(password)
  const pwdOk = Object.values(rules).every(Boolean)

  if (!token) {
    return (
      <div className="auth-shell">
        <div className="auth-card text-center">
          <div style={{ fontSize: 56, marginBottom: 16 }}>🔗</div>
          <h1 className="auth-title">Link không hợp lệ</h1>
          <p className="auth-subtitle" style={{ marginBottom: 24 }}>
            Link đặt lại mật khẩu không hợp lệ hoặc đã hết hạn.
          </p>
          <Link to="/forgot-password" className="btn btn-primary btn-full">
            Yêu cầu link mới
          </Link>
        </div>
      </div>
    )
  }

  const submit = async (e: FormEvent) => {
    e.preventDefault()
    setError('')
    if (!pwdOk) { setError('Mật khẩu không đủ mạnh'); return }
    if (password !== confirm) { setError('Mật khẩu xác nhận không khớp'); return }
    setBusy(true)
    try {
      await auth.resetPassword(token, password)
      setDone(true)
      setTimeout(() => nav('/login', { replace: true }), 3000)
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Có lỗi xảy ra')
    } finally {
      setBusy(false)
    }
  }

  if (done) {
    return (
      <div className="auth-shell">
        <div className="auth-card text-center">
          <div style={{ fontSize: 56, marginBottom: 16 }}>✅</div>
          <h1 className="auth-title">Đặt lại thành công!</h1>
          <p className="auth-subtitle">
            Mật khẩu của bạn đã được cập nhật. Đang chuyển về trang đăng nhập...
          </p>
        </div>
      </div>
    )
  }

  const ruleItem = (ok: boolean, label: string) => (
    <li className={`pwd-rule${ok ? ' ok' : ''}`}>
      <span className="pwd-rule-dot" />
      {label}
    </li>
  )

  return (
    <div className="auth-shell">
      <div className="auth-card">
        <div className="auth-logo">
          <div className="auth-logo-icon">G</div>
          <span className="auth-logo-name">Gokt</span>
        </div>

        <h1 className="auth-title">Đặt lại mật khẩu</h1>
        <p className="auth-subtitle">Nhập mật khẩu mới của bạn bên dưới</p>

        {error && (
          <div className="alert alert-error">
            <span className="alert-icon">⚠️</span>
            {error}
          </div>
        )}

        <form onSubmit={submit}>
          <div className="field">
            <label>Mật khẩu mới</label>
            <div style={{ position: 'relative' }}>
              <input
                type={showPwd ? 'text' : 'password'}
                value={password}
                onChange={(e) => setPassword(e.target.value)}
                placeholder="••••••••"
                required
                autoFocus
                style={{ paddingRight: 44 }}
              />
              <button
                type="button"
                onClick={() => setShowPwd(!showPwd)}
                style={{
                  position: 'absolute', right: 12, top: '50%', transform: 'translateY(-50%)',
                  background: 'none', border: 'none', cursor: 'pointer', fontSize: 16,
                  color: '#94a3b8', padding: 0, height: 'auto',
                }}
              >
                {showPwd ? '🙈' : '👁️'}
              </button>
            </div>
            {password && (
              <ul className="pwd-rules">
                {ruleItem(rules.length,  'Ít nhất 8 ký tự')}
                {ruleItem(rules.upper,   'Chữ hoa (A-Z)')}
                {ruleItem(rules.lower,   'Chữ thường (a-z)')}
                {ruleItem(rules.digit,   'Chữ số (0-9)')}
                {ruleItem(rules.special, 'Ký tự đặc biệt')}
              </ul>
            )}
          </div>

          <div className="field">
            <label>Xác nhận mật khẩu mới</label>
            <input
              type="password"
              value={confirm}
              onChange={(e) => setConfirm(e.target.value)}
              placeholder="••••••••"
              required
            />
            {confirm && confirm !== password && (
              <p className="text-sm mt-1" style={{ color: 'var(--danger)' }}>
                Mật khẩu không khớp
              </p>
            )}
          </div>

          <button type="submit" className="btn btn-primary btn-full mt-2" disabled={busy}>
            {busy ? <span className="spinner" /> : null}
            {busy ? 'Đang cập nhật...' : 'Đặt lại mật khẩu'}
          </button>
        </form>

        <div className="auth-footer">
          <Link to="/login">← Quay về đăng nhập</Link>
        </div>
      </div>
    </div>
  )
}
