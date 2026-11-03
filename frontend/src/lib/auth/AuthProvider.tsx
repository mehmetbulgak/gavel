import { useQueryClient } from '@tanstack/react-query'
import { useCallback, useEffect, useMemo, useRef, useState, type ReactNode } from 'react'
import { api } from '../api/client'
import { onSessionChange, refreshSession, setSession } from '../api/session'
import type { AuthResponse } from '../api/types'
import { AuthContext, type AuthContextValue, type AuthState, type RegisterInput } from './AuthContext'

export function AuthProvider({ children }: { children: ReactNode }) {
  const queryClient = useQueryClient()
  const [state, setState] = useState<AuthState>({ status: 'loading', user: null })
  const userIdRef = useRef<string | null>(null)

  useEffect(() => {
    const unsubscribe = onSessionChange((session) => {
      const userId = session?.user.id ?? null
      if (userId !== userIdRef.current) {
        queryClient.clear()
        userIdRef.current = userId
      }
      setState(session ? { status: 'authenticated', user: session.user } : { status: 'anonymous', user: null })
    })
    void refreshSession()
    return unsubscribe
  }, [queryClient])

  const login = useCallback(async (email: string, password: string) => {
    setSession(await api<AuthResponse>('/api/auth/login', { method: 'POST', body: { email, password } }))
  }, [])

  const register = useCallback(async (input: RegisterInput) => {
    setSession(await api<AuthResponse>('/api/auth/register', { method: 'POST', body: input }))
  }, [])

  const logout = useCallback(async () => {
    try {
      await api('/api/auth/logout', { method: 'POST' })
    } finally {
      setSession(null)
    }
  }, [])

  const value = useMemo<AuthContextValue>(() => ({ ...state, login, register, logout }), [state, login, register, logout])

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}
