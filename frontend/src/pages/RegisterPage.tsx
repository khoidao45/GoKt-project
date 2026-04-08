import { useState } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { GoogleLogin } from '@react-oauth/google'
import type { CredentialResponse } from '@react-oauth/google'
import { auth, setToken } from '../api'
import type { UserDto } from '../types'

interface Props {
  onLogin: (user: UserDto, token: string) => void
}

function checkPwd(p: string) {
  return {
    length:  p.length >= 8,
    upper:   /[A-Z]/.test(p),
    lower:   /[a-z]/.test(p),
    digit:   /\d/.test(p),
    special: /[^A-Za-z0-9]/.test(p),
  }
}

export default function RegisterPage({ onLogin }: Props) {
  const nav = useNavigate()
  const [form, setForm] = useState({
    firstName: '', lastName: '', email: '', phone: '', password: '', confirm: '',
  })
  const [showPwd, setShowPwd] = useState(false)
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState('')

  const set = (k: keyof typeof form) => (e: React.ChangeEvent<HTMLInputElement>) =>
    setForm((f) => ({ ...f, [k]: e.target.value }))

  const rules = checkPwd(form.password)
  const pwdOk = Object.values(rules).every(Boolean)

  const submit = async (e: React.FormEvent) => {
    e.preventDefault()
    setError('')
    if (!pwdOk) { setError('Mật khẩu không đủ mạnh'); return }
    if (form.password !== form.confirm) { setError('Mật khẩu xác nhận không khớp'); return }
    setBusy(true)
    try {
      const res = await auth.register({
        email: form.email,
        password: form.password,
        firstName: form.firstName,
        lastName: form.lastName,
        phone: form.phone || undefined,
      })
      setToken(res.accessToken)
      onLogin(res.user, res.accessToken)
      nav('/dashboard', { replace: true })
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Đăng ký thất bại')
    } finally {
      setBusy(false)
    }
  }

  const handleGoogle = async (cr: CredentialResponse) => {
    if (!cr.credential) return
    setError('')
    setBusy(true)
    try {
      const res = await auth.google(cr.credential)
      setToken(res.accessToken)
      onLogin(res.user, res.accessToken)
      nav('/dashboard', { replace: true })
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Đăng nhập Google thất bại')
    } finally {
      setBusy(false)
    }
  }

  const ruleItem = (ok: boolean, label: string) => (
    <li className={`pwd-rule${ok ? ' ok' : ''}`}>
      <span className="pwd-rule-dot" />
      {label}
    </li>
  )

  return (
    <div className="auth-shell">
      <div className="auth-card" style={{ maxWidth: 480 }}>
        <div className="auth-logo">
          <div className="auth-logo-icon">G</div>
          <span className="auth-logo-name">Gokt</span>
        </div>

        <h1 className="auth-title">Tạo tài khoản</h1>
        <p className="auth-subtitle">Bắt đầu hành trình của bạn với Gokt</p>

        {error && (
          <div className="alert alert-error">
            <span className="alert-icon">⚠️</span>
            {error}
          </div>
        )}

        {/* Google sign-up */}
        <div style={{ marginBottom: 16 }}>
          <GoogleLogin
            onSuccess={handleGoogle}
            onError={() => setError('Đăng nhập Google thất bại')}
            width="100%"
            text="signup_with"
            shape="rectangular"
            theme="outline"
            size="large"
          />
        </div>

        <div className="divider">hoặc đăng ký bằng email</div>

        <form onSubmit={submit}>
          <div className="field-row">
            <div className="field" style={{ marginBottom: 0 }}>
              <label>Họ</label>
              <input
                type="text" value={form.lastName} onChange={set('lastName')}
                placeholder="Nguyễn" required autoFocus
              />
            </div>
            <div className="field" style={{ marginBottom: 0 }}>
              <label>Tên</label>
              <input
                type="text" value={form.firstName} onChange={set('firstName')}
                placeholder="Văn A" required
              />
            </div>
          </div>

          <div className="field mt-4">
            <label>Email</label>
            <input
              type="email" value={form.email} onChange={set('email')}
              placeholder="ban@email.com" required
            />
          </div>

          <div className="field">
            <label>Số điện thoại <span className="text-muted">(tuỳ chọn)</span></label>
            <input
              type="tel" value={form.phone} onChange={set('phone')}
              placeholder="+84 901 234 567"
            />
          </div>

          <div className="field">
            <label>Mật khẩu</label>
            <div style={{ position: 'relative' }}>
              <input
                type={showPwd ? 'text' : 'password'}
                value={form.password} onChange={set('password')}
                placeholder="••••••••" required style={{ paddingRight: 44 }}
              />
              <button
                type="button" onClick={() => setShowPwd(!showPwd)}
                style={{
                  position: 'absolute', right: 12, top: '50%', transform: 'translateY(-50%)',
                  background: 'none', border: 'none', cursor: 'pointer', fontSize: 16,
                  color: '#94a3b8', padding: 0, height: 'auto',
                }}
              >
                {showPwd ? '🙈' : '👁️'}
              </button>
            </div>
            {form.password && (
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
            <label>Xác nhận mật khẩu</label>
            <input
              type="password" value={form.confirm} onChange={set('confirm')}
              placeholder="••••••••" required
            />
            {form.confirm && form.confirm !== form.password && (
              <p className="text-sm mt-1" style={{ color: 'var(--danger)' }}>
                Mật khẩu không khớp
              </p>
            )}
          </div>

          <button type="submit" className="btn btn-primary btn-full mt-2" disabled={busy}>
            {busy ? <span className="spinner" /> : null}
            {busy ? 'Đang tạo tài khoản...' : 'Đăng ký'}
          </button>
        </form>

        <div className="auth-footer">
          Đã có tài khoản?{' '}
          <Link to="/login">Đăng nhập</Link>
        </div>
      </div>
    </div>
  )
}
