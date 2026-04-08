import { useEffect, useState } from 'react'
import { Link, useSearchParams } from 'react-router-dom'
import { auth } from '../api'

export default function VerifyEmailPage() {
  const [params] = useSearchParams()
  const userId = params.get('userId') ?? ''
  const token  = params.get('token')  ?? ''

  const [status, setStatus] = useState<'loading' | 'success' | 'error'>('loading')
  const [message, setMessage] = useState('')

  useEffect(() => {
    if (!userId || !token) {
      setStatus('error')
      setMessage('Link xác thực không hợp lệ hoặc đã hết hạn.')
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
  }, [userId, token])

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
              Link xác thực chỉ có hiệu lực trong 10 phút và chỉ dùng được một lần.
            </div>
            <Link to="/login" className="btn btn-secondary btn-full" style={{ marginBottom: 10 }}>
              Về trang đăng nhập
            </Link>
          </>
        )}
      </div>
    </div>
  )
}
