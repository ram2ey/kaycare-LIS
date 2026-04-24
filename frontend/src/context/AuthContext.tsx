import { createContext, useContext, useState, useCallback, type ReactNode } from 'react'
import type { AuthUser } from '../types/auth'

interface AuthContextValue {
  user: AuthUser | null
  setUser: (u: AuthUser | null) => void
  logout: () => void
}

const AuthContext = createContext<AuthContextValue | null>(null)

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUserState] = useState<AuthUser | null>(() => {
    const raw = localStorage.getItem('lis_auth')
    return raw ? JSON.parse(raw) : null
  })

  const setUser = useCallback((u: AuthUser | null) => {
    setUserState(u)
    if (u) {
      localStorage.setItem('lis_auth', JSON.stringify(u))
    } else {
      localStorage.removeItem('lis_auth')
    }
  }, [])

  const logout = useCallback(() => {
    setUser(null)
  }, [setUser])

  return (
    <AuthContext.Provider value={{ user, setUser, logout }}>
      {children}
    </AuthContext.Provider>
  )
}

export function useAuth() {
  const ctx = useContext(AuthContext)
  if (!ctx) throw new Error('useAuth must be used within AuthProvider')
  return ctx
}
