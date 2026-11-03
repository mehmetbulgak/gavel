import { createContext, useContext } from 'react'
import type { User } from '../api/types'

export type RegisterInput = { displayName: string; email: string; password: string }

export type AuthState = { status: 'loading'; user: null } | { status: 'anonymous'; user: null } | { status: 'authenticated'; user: User }

export type AuthContextValue = AuthState & {
  login: (email: string, password: string) => Promise<void>
  register: (input: RegisterInput) => Promise<void>
  logout: () => Promise<void>
}

export const AuthContext = createContext<AuthContextValue | null>(null)

export function useAuth() {
  const context = useContext(AuthContext)
  if (!context) throw new Error('useAuth must be used inside <AuthProvider>')
  return context
}
