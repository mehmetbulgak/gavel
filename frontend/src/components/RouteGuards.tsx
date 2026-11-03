import type { ReactNode } from 'react'
import { Navigate, useLocation, useSearchParams } from 'react-router'
import { useAuth } from '@/lib/auth/AuthContext'
import { safeReturnPath } from '@/lib/auth/returnPath'
import { Spinner } from './ui/Spinner'

export function RequireAuth({ children }: { children: ReactNode }) {
  const { status } = useAuth()
  const location = useLocation()
  if (status === 'loading') return <Spinner />
  if (status === 'anonymous') {
    return <Navigate to={`/login?next=${encodeURIComponent(location.pathname + location.search)}`} replace />
  }

  return children
}

export function GuestOnly({ children }: { children: ReactNode }) {
  const { status } = useAuth()
  const [params] = useSearchParams()
  if (status === 'loading') return <Spinner fullPage />
  if (status === 'authenticated') return <Navigate to={safeReturnPath(params.get('next'))} replace />
  return children
}
