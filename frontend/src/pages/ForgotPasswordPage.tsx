import { useState, type FormEvent } from 'react'
import { Link } from 'react-router-dom'
import { auth } from '../api'

export default function ForgotPasswordPage() {
  const [email, setEmail] = useState('')
  const [busy, setBusy] = useState(false)
  const [done, setDone] = useState(false)
  const [error, setError] = useState('')

  const submit = async (e: FormEvent) => {
    e.preventDefault()
    setError('')
    setBusy(true)
    try {
      await auth.forgotPassword(email)
      setDone(true)
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Có lỗi xảy ra')
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

        {done ? (
          <div className="text-center">
            <div style={{ fontSize: 56, marginBottom: 16 }}>📬</div>
            <h1 className="auth-title">Kiểm tra email</h1>
            <p className="auth-subtitle" style={{ marginBottom: 24 }}>
              Nếu địa chỉ <strong>{email}</strong> tồn tại trong hệ thống,
              chúng tôi đã gửi link đặt lại mật khẩu. Vui lòng kiểm tra hộp thư (bao gồm Spam).
            </p>
            <Link to="/login" className="btn btn-secondary btn-full">
              ← Quay về đăng nhập
            </Link>
          </div>
        ) : (
          <>
            <h1 className="auth-title">Quên mật khẩu?</h1>
            <p className="auth-subtitle">
              Nhập email của bạn và chúng tôi sẽ gửi link đặt lại mật khẩu.
            </p>

            {error && (
              <div className="alert alert-error">
                <span className="alert-icon">⚠️</span>
                {error}
              </div>
            )}

            <form onSubmit={submit}>
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

              <button type="submit" className="btn btn-primary btn-full mt-2" disabled={busy}>
                {busy ? <span className="spinner" /> : null}
                {busy ? 'Đang gửi...' : 'Gửi link đặt lại mật khẩu'}
              </button>
            </form>

            <div className="auth-footer">
              <Link to="/login">← Quay về đăng nhập</Link>
            </div>
          </>
        )}
      </div>
    </div>
  )
}
