import { Link } from 'react-router'
import type { AppNotification } from '@/lib/api/types'
import { cn } from '@/lib/cn'
import { formatDateTime } from '@/lib/format'
import { notificationIcons } from './notificationIcons'

type NotificationItemProps = { notification: AppNotification; onOpen?: (notification: AppNotification) => void }

export function NotificationItem({ notification, onOpen }: NotificationItemProps) {
  const { icon: Icon, tone } = notificationIcons[notification.type]
  const content = (
    <>
      <span className={cn('mt-0.5 flex size-9 shrink-0 items-center justify-center rounded-full [&_svg]:size-4', tone)}>
        <Icon aria-hidden />
      </span>
      <span className="min-w-0 flex-1">
        <span className={cn('block text-sm', notification.isRead ? 'text-ink-2' : 'font-semibold text-ink')}>{notification.title}</span>
        <span className="mt-0.5 block text-sm text-muted">{notification.body}</span>
        <span className="mt-1 block text-xs text-muted">{formatDateTime(notification.createdAt)}</span>
      </span>
      {!notification.isRead && <span className="mt-2 size-2 shrink-0 rounded-full bg-cobalt-500" aria-label="Unread" />}
    </>
  )
  const className = 'flex w-full gap-3 rounded-xl px-3 py-3 text-left transition-colors hover:bg-ink/4'

  return notification.auctionId ? (
    <Link to={`/auctions/${notification.auctionId}`} className={className} onClick={() => onOpen?.(notification)}>
      {content}
    </Link>
  ) : (
    <button type="button" className={cn(className, 'cursor-pointer')} onClick={() => onOpen?.(notification)}>
      {content}
    </button>
  )
}
