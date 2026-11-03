import * as Popover from '@radix-ui/react-popover'
import { Bell, BellOff } from 'lucide-react'
import { useState } from 'react'
import { Link } from 'react-router'
import type { AppNotification } from '@/lib/api/types'
import { useMarkAllRead, useMarkRead, useNotifications } from './api'
import { NotificationItem } from './NotificationItem'

const PREVIEW_SIZE = 6

export function NotificationBell() {
  const [open, setOpen] = useState(false)
  const { data } = useNotifications({ pageSize: PREVIEW_SIZE })
  const markRead = useMarkRead()
  const markAll = useMarkAllRead()
  const unread = data?.unreadCount ?? 0

  const openNotification = (notification: AppNotification) => {
    if (!notification.isRead) markRead.mutate(notification.id)
    setOpen(false)
  }

  return (
    <Popover.Root open={open} onOpenChange={setOpen}>
      <Popover.Trigger
        className="relative flex size-10 cursor-pointer items-center justify-center rounded-full text-ink-2 hover:bg-ink/5 hover:text-ink"
        aria-label={unread > 0 ? `Notifications, ${unread} unread` : 'Notifications'}
      >
        <Bell className="size-5" aria-hidden />
        {unread > 0 && (
          <span className="tabular absolute top-1 right-1 flex h-4 min-w-4 items-center justify-center rounded-full bg-flare-500 px-1 text-[10px] font-bold text-white">
            {unread > 9 ? '9+' : unread}
          </span>
        )}
      </Popover.Trigger>
      <Popover.Portal>
        <Popover.Content
          align="end"
          sideOffset={8}
          className="z-50 w-[min(24rem,calc(100vw-1.5rem))] rounded-2xl bg-white p-2 shadow-[0_18px_50px_-12px_rgb(22_26_53/0.35)] ring-1 ring-line focus:outline-none"
        >
          <div className="flex items-center justify-between px-3 pt-2 pb-1">
            <p className="font-display font-semibold">Notifications</p>
            {unread > 0 && (
              <button
                type="button"
                className="cursor-pointer text-sm font-medium text-cobalt-600 hover:underline"
                onClick={() => markAll.mutate()}
              >
                Mark all as read
              </button>
            )}
          </div>
          {data && data.items.length > 0 ? (
            <ul className="max-h-[60vh] overflow-y-auto">
              {data.items.map((notification) => (
                <li key={notification.id}>
                  <NotificationItem notification={notification} onOpen={openNotification} />
                </li>
              ))}
            </ul>
          ) : (
            <div className="flex flex-col items-center gap-2 px-6 py-10 text-center text-sm text-muted">
              <BellOff className="size-5" aria-hidden />
              Outbid alerts and results will show up here.
            </div>
          )}
          <Link
            to="/notifications"
            className="mt-1 block rounded-xl px-3 py-2.5 text-center text-sm font-medium text-ink-2 hover:bg-ink/4"
            onClick={() => setOpen(false)}
          >
            See all notifications
          </Link>
        </Popover.Content>
      </Popover.Portal>
    </Popover.Root>
  )
}
