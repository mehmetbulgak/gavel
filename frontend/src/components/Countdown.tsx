import { Clock3 } from 'lucide-react'
import type { AuctionStatus } from '@/lib/api/types'
import { cn } from '@/lib/cn'
import { formatRemaining } from '@/lib/format'
import { useServerNow } from '@/lib/time/useNow'

export const URGENT_MS = 2 * 60 * 1000

type CountdownProps = {
  endsAt: string
  startsAt?: string
  status: AuctionStatus
  className?: string
  urgentClassName?: string
  showIcon?: boolean
}

export function Countdown({ endsAt, startsAt, status, className, urgentClassName = 'text-flare-700', showIcon = true }: CountdownProps) {
  const now = useServerNow()

  if (status === 'Ended' || status === 'Cancelled') {
    return <span className={cn('text-muted', className)}>{status === 'Cancelled' ? 'Cancelled' : 'Ended'}</span>
  }

  if (status === 'Scheduled' && startsAt) {
    return (
      <span className={cn('inline-flex items-center gap-1.5', className)}>
        {showIcon && <Clock3 className="size-4" aria-hidden />}
        Starts in {formatRemaining(Date.parse(startsAt) - now)}
      </span>
    )
  }

  const remaining = Date.parse(endsAt) - now
  const urgent = remaining > 0 && remaining < URGENT_MS

  return (
    <span className={cn('tabular inline-flex items-center gap-1.5', className, urgent && cn('font-semibold', urgentClassName))}>
      {showIcon && <Clock3 className={cn('size-4', urgent && 'animate-live-pulse')} aria-hidden />}
      {remaining > 0 ? `${formatRemaining(remaining)} left` : 'Closing'}
    </span>
  )
}
