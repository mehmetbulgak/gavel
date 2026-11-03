import { Link } from 'react-router'
import { Countdown } from '@/components/Countdown'
import { LotArt } from '@/components/LotArt'
import type { AuctionSummary } from '@/lib/api/types'
import { formatMoney, pluralize } from '@/lib/format'
import { categoryLabels } from './categoryLabels'
import { priceCaption } from './priceCaption'

export function AuctionCard({ auction }: { auction: AuctionSummary }) {
  const ended = auction.status === 'Ended' || auction.status === 'Cancelled'

  return (
    <Link
      to={`/auctions/${auction.id}`}
      className="group flex w-full flex-col overflow-hidden rounded-2xl bg-white ring-1 ring-line transition-shadow hover:ring-ink/25"
    >
      <div className="relative">
        <LotArt id={auction.id} category={auction.category} className="aspect-[16/9] sm:aspect-[4/3]" />
        <span className="absolute top-3 left-3 rounded-full bg-white/85 px-2.5 py-1 text-xs font-medium text-ink-2">
          {categoryLabels[auction.category]}
        </span>
        {!ended && (
          <span className="absolute top-3 right-3 rounded-full bg-white px-2.5 py-1 text-xs">
            <Countdown endsAt={auction.endsAt} startsAt={auction.startsAt} status={auction.status} showIcon={false} />
          </span>
        )}
      </div>
      <div className="flex flex-1 flex-col p-4">
        <h3 className="line-clamp-2 font-sans text-base leading-snug font-medium group-hover:text-cobalt-700">{auction.title}</h3>
        <p className="mt-0.5 text-sm text-muted">by {auction.sellerName}</p>
        <div className="mt-auto flex items-end justify-between gap-3 pt-4">
          <div>
            <p className="text-xs text-muted">{priceCaption(auction)}</p>
            <p
              className={
                ended && auction.outcome !== 'Sold' ? 'font-display text-xl text-muted' : 'tabular font-display text-2xl font-semibold'
              }
            >
              {formatMoney(auction.currentPrice)}
            </p>
          </div>
          <div className="text-right text-sm text-muted">
            <p>{pluralize(auction.bidCount, 'bid')}</p>
            {auction.hasReserve && !auction.reserveMet && !ended && <p className="text-xs">Reserve not met</p>}
          </div>
        </div>
      </div>
    </Link>
  )
}
