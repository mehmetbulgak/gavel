import { Link } from 'react-router'
import { Countdown } from '@/components/Countdown'
import { LotArt } from '@/components/LotArt'
import { PriceTicker } from '@/components/PriceTicker'
import { Button } from '@/components/ui/Button'
import type { AuctionSummary, MarketStats } from '@/lib/api/types'
import { pluralize } from '@/lib/format'
import { priceCaption } from './priceCaption'

type OnTheBlockProps = { auction: AuctionSummary; stats: MarketStats | undefined }

export function OnTheBlock({ auction, stats }: OnTheBlockProps) {
  return (
    <section aria-labelledby="on-the-block" className="relative overflow-hidden rounded-3xl bg-board text-white">
      <div className="grid md:grid-cols-[1.4fr_1fr]">
        <div className="relative z-10 flex flex-col gap-6 p-6 sm:p-10">
          <p id="on-the-block" className="inline-flex items-center gap-2 text-sm font-medium text-white/80">
            <span className="size-2 animate-live-pulse rounded-full bg-flare-500" aria-hidden />
            Closing next
          </p>
          <h2 className="max-w-xl text-3xl leading-tight font-semibold sm:text-4xl">{auction.title}</h2>
          <div>
            <p className="text-sm text-white/70">{priceCaption(auction)}</p>
            <PriceTicker
              value={auction.currentPrice}
              label={priceCaption(auction)}
              className="mt-1 font-display text-6xl font-bold sm:text-7xl"
            />
          </div>
          <div className="flex flex-wrap items-center gap-x-6 gap-y-2 text-white/80">
            <Countdown endsAt={auction.endsAt} status={auction.status} className="text-lg" urgentClassName="text-flare-300" />
            <span>{pluralize(auction.bidCount, 'bid')}</span>
          </div>
          <div>
            <Button asChild variant="board" size="lg">
              <Link to={`/auctions/${auction.id}`}>Open this auction</Link>
            </Button>
          </div>
        </div>
        <LotArt id={auction.id} category={auction.category} size="hero" className="hidden min-h-full opacity-90 md:block" />
      </div>
      {stats && <p className="relative z-10 border-t border-white/10 px-6 py-4 text-sm text-white/70 sm:px-10">{marketSummary(stats)}</p>}
    </section>
  )
}

function marketSummary(stats: MarketStats) {
  const live = `${pluralize(stats.liveAuctions, 'auction')} live right now`
  const bids = `${pluralize(stats.bidsLastHour, 'bid')} in the last hour`
  return stats.soldLastDay > 0 ? `${live}, ${bids} and ${pluralize(stats.soldLastDay, 'item')} sold today.` : `${live} and ${bids}.`
}
