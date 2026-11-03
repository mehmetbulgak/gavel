import { Plus } from 'lucide-react'
import { Link, NavLink, useLocation, useSearchParams } from 'react-router'
import { Button } from '@/components/ui/Button'
import { NotificationBell } from '@/features/notifications/NotificationBell'
import { useAuth } from '@/lib/auth/AuthContext'
import { cn } from '@/lib/cn'
import { Logo } from '../Logo'
import { AccountMenu } from './AccountMenu'

const sections = [
  { status: 'Live', label: 'Live now' },
  { status: 'Upcoming', label: 'Starting soon' },
  { status: 'Ended', label: 'Results' },
] as const

export function Header() {
  const { status } = useAuth()
  const location = useLocation()
  const [params] = useSearchParams()
  const current = location.pathname === '/' ? (params.get('status') ?? 'Live') : null

  return (
    <header className="sticky top-0 z-40 border-b border-line bg-paper/90 backdrop-blur">
      <div className="mx-auto flex max-w-6xl flex-wrap items-center gap-x-6 gap-y-2 px-4 py-3 sm:h-16 sm:flex-nowrap sm:px-6 sm:py-0">
        <Link to="/" className="shrink-0 rounded-lg" aria-label="Gavel home">
          <Logo />
        </Link>

        <nav aria-label="Auctions" className="order-last -mx-1 flex w-full min-w-0 gap-1 overflow-x-auto sm:order-none sm:w-auto sm:flex-1">
          {sections.map((section) => (
            <NavLink
              key={section.status}
              to={section.status === 'Live' ? '/' : `/?status=${section.status}`}
              className={cn(
                'shrink-0 rounded-full px-2.5 py-1.5 text-sm font-medium transition-colors sm:px-3',
                current === section.status ? 'bg-ink text-white' : 'text-ink-2 hover:bg-ink/5 hover:text-ink',
              )}
              aria-current={current === section.status ? 'page' : undefined}
            >
              {section.label}
            </NavLink>
          ))}
        </nav>

        <div className="ml-auto flex shrink-0 items-center gap-1.5 sm:ml-0">
          {status === 'authenticated' ? (
            <>
              <Button asChild size="sm" className="hidden sm:inline-flex">
                <Link to="/sell">
                  <Plus /> Sell an item
                </Link>
              </Button>
              <NotificationBell />
              <AccountMenu />
            </>
          ) : status === 'anonymous' ? (
            <>
              <Button asChild variant="ghost" size="sm">
                <Link to="/login">Sign in</Link>
              </Button>
              <Button asChild size="sm" className="hidden sm:inline-flex">
                <Link to="/register">Create account</Link>
              </Button>
            </>
          ) : null}
        </div>
      </div>
    </header>
  )
}
