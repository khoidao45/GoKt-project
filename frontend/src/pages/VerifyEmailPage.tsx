import { useEffect, useState } from 'react'
import { Link, useSearchParams } from 'react-router-dom'
import { auth } from '../api'

export default function VerifyEmailPage() {
  const [params] = useSearchParams()
  const userId = params.get('userId') ?? ''
  const token = params.get('token') ?? ''
  const initialEmail = params.get('email') ?? ''
  const hasLinkToken = !!userId && !!token

  const [status, setStatus] = useState<'idle' | 'loading' | 'success' | 'error'>(hasLinkToken ? 'loading' : 'idle')
  const [message, setMessage] = useState('')
  const [busy, setBusy] = useState(false)
  const [email, setEmail] = useState(initialEmail)
  const [manualToken, setManualToken] = useState('')

  useEffect(() => {
    if (!hasLinkToken) {
      setStatus('idle')
      return
    }

    auth.verifyEmail(userId, token)
      .then((res) => {
        setMessage(res.message || 'Email đã được xác thực thành công!')
        setStatus('success')
      })
      .catch((err: Error) => {
        setMessage(err.message || 'Xác thực thất bại. Link có thể đã hết hạn.')
        setStatus('error')
      })
  }, [hasLinkToken, token, userId])

  const submitManual = async (e: React.FormEvent) => {
    e.preventDefault()
    if (!email || !manualToken) {
      setStatus('error')
      setMessage('Vui lòng nhập email và mã xác thực.')
      return
    }

    setBusy(true)
    setStatus('loading')
    try {
      const res = await auth.verifyEmailToken(email, manualToken)
      setMessage(res.message || 'Email đã được xác thực thành công!')
      setStatus('success')
    } catch (err) {
      setStatus('error')
      setMessage(err instanceof Error ? err.message : 'Xác thực thất bại.')
    } finally {
      setBusy(false)
    }
  }

  const resend = async () => {
    if (!email) {
      setStatus('error')
      setMessage('Vui lòng nhập email để gửi lại mã xác thực.')
      return
    }

    setBusy(true)
    try {
      const res = await auth.resendVerification(email)
      setStatus('idle')
      setMessage(res.message || 'Đã gửi lại mã xác thực.')
    } catch (err) {
      setStatus('error')
      setMessage(err instanceof Error ? err.message : 'Không thể gửi lại mã xác thực.')
    } finally {
      setBusy(false)
    }
  }

  return (
    <div className="auth-shell">
      <div className="auth-card text-center">
        <div className="auth-logo" style={{ justifyContent: 'center', marginBottom: 24 }}>
          <div className="auth-logo-icon">G</div>
          <span className="auth-logo-name">Gokt</span>
        </div>

        {status === 'loading' && (
          <>
            <div style={{ fontSize: 56, marginBottom: 16 }}>🔄</div>
            <h1 className="auth-title">Đang xác thực...</h1>
            <p className="auth-subtitle">Vui lòng chờ trong giây lát</p>
            <div style={{ display: 'flex', justifyContent: 'center', marginTop: 16 }}>
              <span className="spinner spinner-dark" style={{ width: 32, height: 32, borderWidth: 3 }} />
            </div>
          </>
        )}

        {status === 'idle' && (
          <>
            <div style={{ fontSize: 56, marginBottom: 16 }}>✉️</div>
            <h1 className="auth-title">Xác thực email</h1>
            <p className="auth-subtitle" style={{ marginBottom: 18 }}>
              Nhập mã xác thực đã gửi vào email của bạn. Tài khoản chưa xác thực sẽ bị xoá sau 10 phút.
            </p>

            {message && (
              <div className="alert alert-success" style={{ textAlign: 'left', marginBottom: 16 }}>
                <span className="alert-icon">ℹ️</span>
                {message}
              </div>
            )}

            <form onSubmit={submitManual} style={{ textAlign: 'left' }}>
              <div className="field">
                <label>Email</label>
                <input
                  type="email"
                  value={email}
                  onChange={(e) => setEmail(e.target.value)}
                  placeholder="ban@email.com"
                  required
                />
              </div>
              <div className="field">
                <label>Mã xác thực</label>
                <input
                  type="text"
                  value={manualToken}
                  onChange={(e) => setManualToken(e.target.value)}
                  placeholder="Dán mã token từ email"
                  required
                />
              </div>
              <button className="btn btn-primary btn-full" disabled={busy} type="submit">
                {busy ? 'Đang xác thực...' : 'Xác thực email'}
              </button>
            </form>

            <button
              type="button"
              onClick={resend}
              className="btn btn-secondary btn-full"
              style={{ marginTop: 10 }}
              disabled={busy}
            >
              Gửi lại mã xác thực
            </button>

            <Link to="/login" className="btn btn-ghost btn-full" style={{ marginTop: 10 }}>
              Về trang đăng nhập
            </Link>
          </>
        )}

        {status === 'success' && (
          <>
            <div style={{ fontSize: 56, marginBottom: 16 }}>✅</div>
            <h1 className="auth-title">Xác thực thành công!</h1>
            <p className="auth-subtitle" style={{ marginBottom: 24 }}>{message}</p>
            <div className="alert alert-success" style={{ textAlign: 'left', marginBottom: 20 }}>
              <span className="alert-icon">🎉</span>
              Email của bạn đã được xác nhận. Bạn có thể đăng nhập bình thường.
            </div>
            <Link to="/login" className="btn btn-primary btn-full">
              Đăng nhập ngay
            </Link>
          </>
        )}

        {status === 'error' && (
          <>
            <div style={{ fontSize: 56, marginBottom: 16 }}>❌</div>
            <h1 className="auth-title">Xác thực thất bại</h1>
            <p className="auth-subtitle" style={{ marginBottom: 24 }}>{message}</p>
            <div className="alert alert-error" style={{ textAlign: 'left', marginBottom: 20 }}>
              <span className="alert-icon">⚠️</span>
              Link/mã xác thực chỉ có hiệu lực trong 10 phút và chỉ dùng được một lần.
            </div>
            <button type="button" className="btn btn-secondary btn-full" style={{ marginBottom: 10 }} onClick={resend}>
              Gửi lại mã xác thực
            </button>
            <Link to="/login" className="btn btn-secondary btn-full" style={{ marginBottom: 10 }}>
              Về trang đăng nhập
            </Link>
          </>
        )}
      </div>
    </div>
  )
}
