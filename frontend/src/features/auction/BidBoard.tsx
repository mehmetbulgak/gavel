import { Link, useLocation } from 'react-router'
import { Countdown } from '@/components/Countdown'
import { Paddle } from '@/components/Paddle'
import { PriceTicker } from '@/components/PriceTicker'
import { Button } from '@/components/ui/Button'
import { formatDuration } from '@/features/rules/formatDuration'
import type { AuctionDetails } from '@/lib/api/types'
import { useAuth } from '@/lib/auth/AuthContext'
import { formatDateTime, formatMoney, pluralize } from '@/lib/format'
import { priceLabel, viewerStanding } from './auctionState'
import { BidForm } from './BidForm'
import { ViewerBanner } from './ViewerBanner'

const statusLine: Record<AuctionDetails['status'], string> = {
  Live: 'Live auction',
  Scheduled: 'Not open yet',
  Ended: 'Auction closed',
  Cancelled: 'Withdrawn by the seller',
}

type BidBoardProps = { auction: AuctionDetails; snipeSeconds: number | undefined }

export function BidBoard({ auction, snipeSeconds }: BidBoardProps) {
  const { status } = useAuth()
  const location = useLocation()
  const standing = viewerStanding(auction)
  const label = priceLabel(auction)
  const live = auction.status === 'Live'
  const canBid = live && status === 'authenticated' && !auction.viewer?.isSeller
  const leaderIsYou = Boolean(auction.viewer?.isLeading || auction.viewer?.isWinner)

  return (
    <section aria-labelledby="bid-board-heading" className="grid gap-6 rounded-3xl bg-board p-6 text-white sm:p-7">
      <h2 id="bid-board-heading" className="flex items-center gap-2 text-sm font-medium text-white/80">
        {live && <span className="size-2 animate-live-pulse rounded-full bg-flare-500" aria-hidden />}
        {statusLine[auction.status]}
      </h2>

      <div>
        <p className="text-sm text-white/70">{label}</p>
        <PriceTicker
          value={auction.finalPrice ?? auction.currentPrice}
          label={label}
          className="mt-1.5 font-display text-5xl font-bold sm:text-6xl"
        />
        <div className="mt-3 flex flex-wrap items-center gap-x-4 gap-y-2 text-sm text-white/80">
          {auction.leaderAlias && auction.bidCount > 0 && (
            <span className="inline-flex items-center gap-1.5">
              <Paddle alias={auction.leaderAlias} isYou={leaderIsYou} tone="board" />
              {leaderVerb(auction, leaderIsYou)}
            </span>
          )}
          <span>{pluralize(auction.bidCount, 'bid')}</span>
          {auction.hasReserve && (
            <span className={auction.reserveMet ? 'text-mint-300' : 'text-flare-300'}>
              {auction.reserveMet ? 'Reserve met' : 'Reserve not met'}
            </span>
          )}
        </div>
      </div>

      {(live || auction.status === 'Scheduled') && (
        <div className="grid gap-1">
          <Countdown
            endsAt={auction.endsAt}
            startsAt={auction.startsAt}
            status={auction.status}
            className="text-2xl font-semibold"
            urgentClassName="text-flare-300"
          />
          <p className="text-sm text-white/60">
            {auction.status === 'Scheduled'
              ? `Bidding opens ${formatDateTime(auction.startsAt)}.`
              : snipeSeconds
                ? `A bid in the last ${formatDuration(snipeSeconds / 60)} resets the clock to ${formatDuration(snipeSeconds / 60)}.`
                : `Closes ${formatDateTime(auction.endsAt)}.`}
            {auction.extensionCount > 0 && ` Late bids have extended it ${pluralize(auction.extensionCount, 'time')}.`}
          </p>
        </div>
      )}

      {auction.status === 'Ended' && <EndedSummary auction={auction} />}

      <ViewerBanner standing={standing} />

      {canBid && <BidForm auction={auction} />}

      {live && status === 'anonymous' && (
        <div className="grid gap-2">
          <Button asChild variant="board" size="lg">
            <Link to={`/login?next=${encodeURIComponent(location.pathname)}`}>Sign in to bid</Link>
          </Button>
          <p className="text-center text-sm text-white/60">The next bid must be at least {formatMoney(auction.minimumNextBid)}.</p>
        </div>
      )}
    </section>
  )
}

function leaderVerb(auction: AuctionDetails, isYou: boolean) {
  if (auction.status !== 'Ended') return isYou ? 'lead' : 'leads'
  return auction.outcome === 'Sold' ? 'won' : 'bid highest'
}

function EndedSummary({ auction }: { auction: AuctionDetails }) {
  const text =
    auction.outcome === 'Sold'
      ? `Closed ${formatDateTime(auction.endsAt)} and sold to the highest bidder.`
      : auction.outcome === 'ReserveNotMet'
        ? `Closed ${formatDateTime(auction.endsAt)}. Bidding stopped below the seller's reserve, so the item was not sold.`
        : `Closed ${formatDateTime(auction.endsAt)} without any bids.`

  return <p className="text-sm text-white/70">{text}</p>
}
