import { Suspense } from 'react'
import { Outlet } from 'react-router'
import { useLiveNotifications } from '@/features/notifications/useLiveNotifications'
import { useAuth } from '@/lib/auth/AuthContext'
import { Spinner } from '../ui/Spinner'
import { Header } from './Header'

export function AppShell() {
  const { user } = useAuth()
  useLiveNotifications(user?.id ?? null)

  return (
    <div className="flex min-h-dvh flex-col">
      <Header />
      <main className="mx-auto w-full max-w-6xl flex-1 px-4 py-8 sm:px-6 lg:py-10">
        <Suspense fallback={<Spinner />}>
          <Outlet />
        </Suspense>
      </main>
      <footer className="border-t border-line">
        <div className="mx-auto flex max-w-6xl flex-wrap items-center justify-between gap-3 px-4 py-6 text-sm text-muted sm:px-6">
          <p>Simulated auctions with virtual bids. No items are sold or shipped.</p>
        </div>
      </footer>
    </div>
  )
}
