import { useState, useEffect } from 'react'
import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom'
import { GoogleOAuthProvider } from '@react-oauth/google'
import { getToken, setToken, users } from './api'
import type { UserDto } from './types'

const GOOGLE_CLIENT_ID = import.meta.env.VITE_GOOGLE_CLIENT_ID ?? ''
const GOOGLE_AUTH_ENABLED = GOOGLE_CLIENT_ID.trim().length > 0

const roleHome = (roles?: string[]) =>
  roles?.includes('ADMIN') ? '/admin'
  : roles?.includes('DRIVER') ? '/driver'
  : '/dashboard'

import LoginPage from './pages/LoginPage'
import RegisterPage from './pages/RegisterPage'
import ForgotPasswordPage from './pages/ForgotPasswordPage'
import ResetPasswordPage from './pages/ResetPasswordPage'
import VerifyEmailPage from './pages/VerifyEmailPage'
import DashboardPage from './pages/DashboardPage'
import DriverDashboardPage from './pages/DriverDashboardPage'
import AdminDashboardPage from './pages/AdminDashboardPage'

export default function App() {
  const [user, setUser] = useState<UserDto | null>(null)
  const [booting, setBooting] = useState(() => Boolean(getToken()))

  // Restore session from localStorage on first load
  useEffect(() => {
    const token = getToken()
    if (!token) return

    users.me()
      .then((u) => setUser(u))
      .catch(() => {
        // token expired or invalid — clear it
        setToken('')
      })
      .finally(() => setBooting(false))
  }, [])

  const handleLogin = (u: UserDto, token: string) => {
    setToken(token)
    setUser(u)
  }

  const handleLogout = () => {
    setToken('')
    setUser(null)
  }

  if (booting) {
    return (
      <div style={{
        minHeight: '100vh', display: 'flex', alignItems: 'center',
        justifyContent: 'center', background: '#0f172a',
      }}>
        <div style={{ textAlign: 'center', color: '#94a3b8' }}>
          <div style={{
            width: 48, height: 48, borderRadius: 12,
            background: 'linear-gradient(135deg, #6366f1, #8b5cf6)',
            display: 'flex', alignItems: 'center', justifyContent: 'center',
            fontSize: 24, fontWeight: 700, color: '#fff', margin: '0 auto 16px',
          }}>G</div>
          <div style={{ fontSize: 14 }}>Đang tải...</div>
        </div>
      </div>
    )
  }

  const appRoutes = (
    <BrowserRouter>
      <Routes>
        {/* Public routes */}
        <Route
          path="/login"
          element={user ? <Navigate to={roleHome(user.roles)} replace /> : <LoginPage onLogin={handleLogin} googleEnabled={GOOGLE_AUTH_ENABLED} />}
        />
        <Route
          path="/register"
          element={user ? <Navigate to={roleHome(user.roles)} replace /> : <RegisterPage onLogin={handleLogin} googleEnabled={GOOGLE_AUTH_ENABLED} />}
        />
        <Route path="/forgot-password" element={<ForgotPasswordPage />} />
        <Route path="/reset-password"  element={<ResetPasswordPage />} />
        <Route path="/verify-email"    element={<VerifyEmailPage />} />

        {/* Protected routes */}
        <Route
          path="/dashboard"
          element={
            user
              ? <DashboardPage user={user} onLogout={handleLogout} />
              : <Navigate to="/login" replace />
          }
        />
        <Route
          path="/driver"
          element={
            user
              ? <DriverDashboardPage user={user} onLogout={handleLogout} />
              : <Navigate to="/login" replace />
          }
        />
        <Route
          path="/admin"
          element={
            user
              ? user.roles?.includes('ADMIN')
                ? <AdminDashboardPage user={user} onLogout={handleLogout} />
                : <Navigate to="/dashboard" replace />
              : <Navigate to="/login" replace />
          }
        />

        {/* Default redirect */}
        <Route
          path="*"
          element={<Navigate to={user ? roleHome(user.roles) : '/login'} replace />}
        />
      </Routes>
    </BrowserRouter>
  )

  return GOOGLE_AUTH_ENABLED
    ? <GoogleOAuthProvider clientId={GOOGLE_CLIENT_ID}>{appRoutes}</GoogleOAuthProvider>
    : appRoutes
}
