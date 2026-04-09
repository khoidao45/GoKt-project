import { useState } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { GoogleLogin } from '@react-oauth/google'
import { auth, setToken } from '../api'
import type { UserDto } from '../types'

interface Props {
  onLogin: (user: UserDto, token: string) => void
  googleEnabled?: boolean
}

export default function LoginPage({ onLogin, googleEnabled = false }: Props) {
  const nav = useNavigate()
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [showPwd, setShowPwd] = useState(false)
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState('')

  const handleLogin = async (e: React.FormEvent) => {
    e.preventDefault()
    setError('')
    setBusy(true)
    try {
      const res = await auth.login(email, password)
      setToken(res.accessToken)
      onLogin(res.user, res.accessToken)
      nav(res.user.roles?.includes('DRIVER') ? '/driver' : '/dashboard', { replace: true })
    } catch (err) {
      const msg = err instanceof Error ? err.message : 'Đăng nhập thất bại'
      if (msg.includes('EMAIL_NOT_VERIFIED')) {
        nav(`/verify-email?email=${encodeURIComponent(email)}`, { replace: true })
        return
      }
      setError(msg)
    } finally {
      setBusy(false)
    }
  }

  const handleGoogle = async (credentialResponse: import('@react-oauth/google').CredentialResponse) => {
    if (!credentialResponse.credential) return
    setError('')
    setBusy(true)
    try {
      const res = await auth.google(credentialResponse.credential)
      setToken(res.accessToken)
      onLogin(res.user, res.accessToken)
      nav(res.user.roles?.includes('DRIVER') ? '/driver' : '/dashboard', { replace: true })
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Đăng nhập Google thất bại')
    } finally {
      setBusy(false)
    }
  }

  return (
    <div className="auth-shell">
      <div className="auth-card">
        <div className="auth-logo">
          <div className="auth-logo-icon">G</div>
          <span className="auth-logo-name">Gokt</span>
        </div>

        <h1 className="auth-title">Chào mừng trở lại</h1>
        <p className="auth-subtitle">Đăng nhập để tiếp tục hành trình của bạn</p>

        {error && (
          <div className="alert alert-error">
            <span className="alert-icon">⚠️</span>
            {error}
          </div>
        )}

        {googleEnabled && (
          <>
            <div style={{ marginBottom: 16 }}>
              <GoogleLogin
                onSuccess={handleGoogle}
                onError={() => setError('Đăng nhập Google thất bại')}
                width="100%"
                text="signin_with"
                shape="rectangular"
                theme="outline"
                size="large"
              />
            </div>
            <div className="divider">hoặc đăng nhập bằng email</div>
          </>
        )}

        <form onSubmit={handleLogin}>
          <div className="field">
            <label>Email</label>
            <input
              type="email"
              value={email}
              onChange={(e) => setEmail(e.target.value)}
              placeholder="ban@email.com"
              required
              autoFocus
            />
          </div>

          <div className="field">
            <label style={{ display: 'flex', justifyContent: 'space-between' }}>
              Mật khẩu
              <Link to="/forgot-password" style={{ fontSize: 12, fontWeight: 500 }}>
                Quên mật khẩu?
              </Link>
            </label>
            <div style={{ position: 'relative' }}>
              <input
                type={showPwd ? 'text' : 'password'}
                value={password}
                onChange={(e) => setPassword(e.target.value)}
                placeholder="••••••••"
                required
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
          </div>

          <button type="submit" className="btn btn-primary btn-full mt-2" disabled={busy}>
            {busy ? <span className="spinner" /> : null}
            {busy ? 'Đang đăng nhập...' : 'Đăng nhập'}
          </button>
        </form>

        <div className="auth-footer">
          Chưa có tài khoản?{' '}
          <Link to="/register">Đăng ký ngay</Link>
        </div>
      </div>
    </div>
  )
}
