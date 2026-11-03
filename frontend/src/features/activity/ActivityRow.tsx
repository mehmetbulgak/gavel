import type { ReactNode } from 'react'
import { Link } from 'react-router'
import { Countdown } from '@/components/Countdown'
import { LotArt } from '@/components/LotArt'
import { priceCaption } from '@/features/browse/priceCaption'
import type { AuctionSummary } from '@/lib/api/types'
import { formatDateTime, formatMoney, pluralize } from '@/lib/format'

type ActivityRowProps = { auction: AuctionSummary; badge?: ReactNode; detail?: ReactNode }

export function ActivityRow({ auction, badge, detail }: ActivityRowProps) {
  const open = auction.status === 'Live' || auction.status === 'Scheduled'

  return (
    <Link
      to={`/auctions/${auction.id}`}
      className="group flex items-center gap-4 rounded-2xl bg-white p-3 ring-1 ring-line transition-shadow hover:ring-ink/25 sm:p-4"
    >
      <LotArt id={auction.id} category={auction.category} className="size-16 shrink-0 rounded-xl sm:size-20" />
      <div className="min-w-0 flex-1">
        <p className="truncate font-medium group-hover:text-cobalt-700">{auction.title}</p>
        <p className="mt-1 flex flex-wrap items-center gap-x-3 gap-y-1 text-sm text-muted">
          {open ? (
            <Countdown endsAt={auction.endsAt} startsAt={auction.startsAt} status={auction.status} />
          ) : (
            <span>
              {auction.status === 'Cancelled' ? 'Withdrawn' : 'Closed'} {formatDateTime(auction.endsAt)}
            </span>
          )}
          <span>{pluralize(auction.bidCount, 'bid')}</span>
          {detail}
        </p>
      </div>
      <div className="flex shrink-0 flex-col items-end gap-1.5 text-right">
        <p className="text-xs text-muted">{priceCaption(auction)}</p>
        <p className="tabular font-display text-lg font-semibold">{formatMoney(auction.currentPrice)}</p>
        {badge}
      </div>
    </Link>
  )
}
