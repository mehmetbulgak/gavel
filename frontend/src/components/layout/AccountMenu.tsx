import * as Popover from '@radix-ui/react-popover'
import { LogOut, Plus, Rows3 } from 'lucide-react'
import { useState } from 'react'
import { Link } from 'react-router'
import { useAuth } from '@/lib/auth/AuthContext'

export function AccountMenu() {
  const { user, logout } = useAuth()
  const [open, setOpen] = useState(false)
  if (!user) return null

  const initials = user.displayName
    .split(' ')
    .map((part) => part[0])
    .join('')
    .slice(0, 2)
    .toUpperCase()

  return (
    <Popover.Root open={open} onOpenChange={setOpen}>
      <Popover.Trigger
        className="flex size-10 cursor-pointer items-center justify-center rounded-full bg-ink font-display text-sm font-semibold text-white"
        aria-label={`Account menu for ${user.displayName}`}
      >
        {initials}
      </Popover.Trigger>
      <Popover.Portal>
        <Popover.Content
          align="end"
          sideOffset={8}
          className="z-50 w-64 rounded-2xl bg-white p-2 shadow-[0_18px_50px_-12px_rgb(22_26_53/0.35)] ring-1 ring-line focus:outline-none"
        >
          <div className="px-3 py-2">
            <p className="truncate font-medium">{user.displayName}</p>
            <p className="truncate text-sm text-muted">{user.email}</p>
          </div>
          <Link
            to="/sell"
            onClick={() => setOpen(false)}
            className="flex items-center gap-2.5 rounded-xl px-3 py-2.5 text-sm text-ink-2 hover:bg-ink/4 hover:text-ink sm:hidden"
          >
            <Plus className="size-4" aria-hidden /> Sell an item
          </Link>
          <Link
            to="/activity"
            onClick={() => setOpen(false)}
            className="flex items-center gap-2.5 rounded-xl px-3 py-2.5 text-sm text-ink-2 hover:bg-ink/4 hover:text-ink"
          >
            <Rows3 className="size-4" aria-hidden /> My activity
          </Link>
          <button
            type="button"
            onClick={() => void logout()}
            className="flex w-full cursor-pointer items-center gap-2.5 rounded-xl px-3 py-2.5 text-sm text-ink-2 hover:bg-ink/4 hover:text-ink"
          >
            <LogOut className="size-4" aria-hidden /> Sign out
          </button>
        </Popover.Content>
      </Popover.Portal>
    </Popover.Root>
  )
}
