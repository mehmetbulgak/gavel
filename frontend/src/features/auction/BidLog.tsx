import { useState } from 'react'
import { Paddle } from '@/components/Paddle'
import type { Bid } from '@/lib/api/types'
import { cn } from '@/lib/cn'
import { formatClock, formatMoney, formatNumber, pluralize } from '@/lib/format'

const COLLAPSED = 8

type BidLogProps = { bids: readonly Bid[]; totalCount: number }

export function BidLog({ bids, totalCount }: BidLogProps) {
  const [seen] = useState(() => new Set(bids.map((bid) => bid.id)))
  const [expanded, setExpanded] = useState(false)
  const visible = expanded ? bids : bids.slice(0, COLLAPSED)
  const hidden = bids.length - visible.length

  return (
    <div>
      <ol className="divide-y divide-line" aria-live="polite" aria-relevant="additions">
        {visible.map((bid, index) => (
          <li key={bid.id} className={cn('flex items-center gap-3 rounded-lg px-2 py-2.5', !seen.has(bid.id) && 'animate-bid-flash')}>
            <Paddle alias={bid.alias} isYou={bid.isMine} className={cn('w-28 shrink-0', index === 0 ? 'text-ink' : 'text-ink-2')} />
            <span className="min-w-0 flex-1 truncate text-sm text-muted">
              {bid.isAutomatic ? 'Automatic, from a maximum bid' : index === 0 ? 'Highest bid' : ''}
            </span>
            <span className={cn('tabular text-right', index === 0 ? 'font-semibold text-ink' : 'text-ink-2')}>
              {formatMoney(bid.amount)}
            </span>
            <time dateTime={bid.placedAt} className="tabular hidden w-20 text-right text-sm text-muted sm:block">
              {formatClock(bid.placedAt)}
            </time>
          </li>
        ))}
      </ol>
      {(hidden > 0 || totalCount > bids.length) && (
        <div className="flex flex-wrap items-center justify-between gap-2 pt-3 text-sm text-muted">
          {hidden > 0 ? (
            <button type="button" className="cursor-pointer font-medium text-cobalt-600 hover:underline" onClick={() => setExpanded(true)}>
              Show {pluralize(hidden, 'earlier bid')}
            </button>
          ) : (
            <span />
          )}
          {totalCount > bids.length && (
            <span>
              Showing the latest {formatNumber(bids.length)} of {formatNumber(totalCount)} bids
            </span>
          )}
        </div>
      )}
    </div>
  )
}
