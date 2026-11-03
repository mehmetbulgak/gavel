import { AlertTriangle, BellOff, CheckCheck } from 'lucide-react'
import { Button } from '@/components/ui/Button'
import { EmptyState } from '@/components/ui/EmptyState'
import { Pagination } from '@/components/ui/Pagination'
import { SegmentedControl } from '@/components/ui/SegmentedControl'
import { Skeleton } from '@/components/ui/Skeleton'
import type { AppNotification } from '@/lib/api/types'
import { pluralize } from '@/lib/format'
import { useUrlFilters } from '@/lib/hooks/useUrlFilters'
import { useMarkAllRead, useMarkRead, useNotifications } from './api'
import { NotificationItem } from './NotificationItem'

const PAGE_SIZE = 20

const FILTERS = [
  { value: 'all', label: 'All' },
  { value: 'unread', label: 'Unread' },
] as const

export default function NotificationsPage() {
  const { params, update, page } = useUrlFilters()
  const filter = params.get('filter') === 'unread' ? 'unread' : 'all'
  const notifications = useNotifications({ unreadOnly: filter === 'unread' || undefined, page, pageSize: PAGE_SIZE })
  const markRead = useMarkRead()
  const markAll = useMarkAllRead()
  const unread = notifications.data?.unreadCount ?? 0
  const totalPages = notifications.data ? Math.max(1, Math.ceil(notifications.data.totalCount / PAGE_SIZE)) : 1

  const open = (notification: AppNotification) => {
    if (!notification.isRead) markRead.mutate(notification.id)
  }

  return (
    <div className="mx-auto grid w-full max-w-2xl grid-cols-1 gap-6">
      <title>Notifications | Gavel</title>
      <div className="flex flex-wrap items-end justify-between gap-4">
        <div>
          <h1 className="text-3xl font-bold tracking-tight">Notifications</h1>
          <p className="mt-1 text-muted">{unread > 0 ? `${pluralize(unread, 'unread notification')}.` : 'You are all caught up.'}</p>
        </div>
        {unread > 0 && (
          <Button variant="secondary" size="sm" loading={markAll.isPending} onClick={() => markAll.mutate()}>
            <CheckCheck /> Mark all as read
          </Button>
        )}
      </div>
      <SegmentedControl
        label="Show"
        value={filter}
        options={FILTERS}
        onChange={(value) => update({ filter: value === 'all' ? null : value })}
        className="w-fit"
      />

      {notifications.isError ? (
        <EmptyState icon={<AlertTriangle />} title="Notifications could not be loaded" description="Refresh the page to try again." />
      ) : notifications.isPending ? (
        <div className="grid gap-2">
          {Array.from({ length: 5 }, (_, i) => (
            <Skeleton key={i} className="h-20" />
          ))}
        </div>
      ) : notifications.data.items.length === 0 ? (
        <EmptyState
          icon={<BellOff />}
          title={filter === 'unread' ? 'No unread notifications' : 'No notifications yet'}
          description="Outbid alerts, closing reminders and results show up here as they happen."
        />
      ) : (
        <div>
          <ul className="grid grid-cols-1 gap-1 rounded-3xl bg-white p-2 ring-1 ring-line">
            {notifications.data.items.map((notification) => (
              <li key={notification.id}>
                <NotificationItem notification={notification} onOpen={open} />
              </li>
            ))}
          </ul>
          <Pagination page={page} totalPages={totalPages} onPageChange={(next) => update({ page: String(next) })} />
        </div>
      )}
    </div>
  )
}
