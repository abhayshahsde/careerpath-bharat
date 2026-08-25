'use client'

import { createContext, useContext, useEffect, useState, useCallback, ReactNode } from 'react'

interface AuthUser {
  id: string
  email: string
  displayName: string | null
  roles: string[]
}

interface AuthContextValue {
  user: AuthUser | null
  isLoading: boolean
  login: (token: string, refreshToken: string, user: AuthUser) => void
  logout: () => void
  isAuthenticated: boolean
}

const AuthContext = createContext<AuthContextValue | null>(null)

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<AuthUser | null>(null)
  const [isLoading, setIsLoading] = useState(true)

  useEffect(() => {
    // Rehydrate from localStorage on mount
    try {
      const stored = localStorage.getItem('auth_user')
      const token  = localStorage.getItem('access_token')
      if (stored && token) {
        setUser(JSON.parse(stored))
      }
    } catch {
      // ignore parse errors
    } finally {
      setIsLoading(false)
    }
  }, [])

  const login = useCallback((token: string, refreshToken: string, authUser: AuthUser) => {
    localStorage.setItem('access_token', token)
    localStorage.setItem('refresh_token', refreshToken)
    localStorage.setItem('auth_user', JSON.stringify(authUser))
    setUser(authUser)
  }, [])

  const logout = useCallback(() => {
    localStorage.removeItem('access_token')
    localStorage.removeItem('refresh_token')
    localStorage.removeItem('auth_user')
    setUser(null)
  }, [])

  return (
    <AuthContext.Provider value={{ user, isLoading, login, logout, isAuthenticated: !!user }}>
      {children}
    </AuthContext.Provider>
  )
}

export function useAuth() {
  const ctx = useContext(AuthContext)
  if (!ctx) throw new Error('useAuth must be used inside <AuthProvider>')
  return ctx
}
