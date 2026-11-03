import { AlertTriangle, ArrowLeft, Gavel, SearchX } from 'lucide-react'
import { Link, useParams } from 'react-router'
import { LotArt } from '@/components/LotArt'
import { Button } from '@/components/ui/Button'
import { EmptyState } from '@/components/ui/EmptyState'
import { Skeleton } from '@/components/ui/Skeleton'
import { categoryLabels } from '@/features/browse/categoryLabels'
import { useAuctionRules } from '@/features/rules/api'
import { ApiError } from '@/lib/api/errors'
import type { AuctionDetails } from '@/lib/api/types'
import { useAuth } from '@/lib/auth/AuthContext'
import { formatMoney } from '@/lib/format'
import { useLiveAuction } from '@/lib/realtime/AuctionLiveContext'
import { useAuction, useBidHistory } from './api'
import { AuctionFacts } from './AuctionFacts'
import { isOpen } from './auctionState'
import { BidBoard } from './BidBoard'
import { BidLog } from './BidLog'
import { PriceHistory } from './PriceHistory'
import { WatchButton } from './WatchButton'
import { WithdrawButton } from './WithdrawButton'

const GUID = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i

export default function AuctionPage() {
  const { id = '' } = useParams()
  return GUID.test(id) ? <AuctionView key={id} id={id} /> : <MissingAuction />
}

function AuctionView({ id }: { id: string }) {
  useLiveAuction(id)
  const auction = useAuction(id)

  if (auction.isPending) return <AuctionSkeleton />
  if (auction.isError) {
    return auction.error instanceof ApiError && auction.error.status === 404 ? (
      <MissingAuction />
    ) : (
      <EmptyState
        icon={<AlertTriangle />}
        title="This auction could not be loaded"
        description="Check your connection and refresh the page."
      />
    )
  }

  return <AuctionLayout auction={auction.data} />
}

function AuctionLayout({ auction }: { auction: AuctionDetails }) {
  const rules = useAuctionRules()

  return (
    <article className="grid grid-cols-1 gap-8 lg:grid-cols-[minmax(0,1fr)_380px] lg:items-start lg:gap-10">
      <title>{`${auction.title} | Gavel`}</title>

      <header className="grid gap-5">
        <Link to="/" className="inline-flex w-fit items-center gap-1.5 text-sm font-medium text-ink-2 hover:text-ink">
          <ArrowLeft className="size-4" aria-hidden /> All auctions
        </Link>
        <div>
          <p className="text-sm font-medium text-cobalt-700">{categoryLabels[auction.category]}</p>
          <h1 className="mt-1 text-3xl leading-tight font-bold tracking-tight sm:text-4xl">{auction.title}</h1>
          <p className="mt-2 text-muted">Listed by {auction.sellerName}</p>
        </div>
        <LotArt id={auction.id} category={auction.category} size="hero" className="aspect-[16/9] rounded-3xl" />
      </header>

      <aside className="grid gap-4 lg:sticky lg:top-24 lg:col-start-2 lg:row-span-2 lg:row-start-1">
        <BidBoard auction={auction} snipeSeconds={rules.data?.snipeProtectionSeconds} />
        <AuctionActions auction={auction} />
        <AuctionFacts auction={auction} />
      </aside>

      <div className="grid min-w-0 grid-cols-1 gap-10">
        {auction.description && (
          <section aria-labelledby="description-heading">
            <h2 id="description-heading" className="font-display text-xl font-semibold">
              About this item
            </h2>
            <p className="mt-3 max-w-prose leading-relaxed whitespace-pre-line text-ink-2">{auction.description}</p>
          </section>
        )}
        <BiddingActivity auction={auction} />
      </div>
    </article>
  )
}

function AuctionActions({ auction }: { auction: AuctionDetails }) {
  const { status } = useAuth()
  if (!isOpen(auction)) return null

  if (auction.viewer?.isSeller) {
    return auction.bidCount === 0 ? (
      <div className="flex">
        <WithdrawButton auctionId={auction.id} />
      </div>
    ) : null
  }

  if (status === 'authenticated' && auction.viewer) {
    return (
      <div className="flex">
        <WatchButton auctionId={auction.id} watching={auction.viewer.isWatching} />
      </div>
    )
  }

  return null
}

function BiddingActivity({ auction }: { auction: AuctionDetails }) {
  const history = useBidHistory(auction.id)

  return (
    <section aria-labelledby="activity-heading" className="grid grid-cols-1 gap-5">
      <h2 id="activity-heading" className="font-display text-xl font-semibold">
        Bidding so far
      </h2>
      {history.isPending ? (
        <Skeleton className="h-56" />
      ) : history.isError ? (
        <p className="text-muted">Bid history could not be loaded.</p>
      ) : history.data.items.length === 0 ? (
        <div className="flex items-center gap-4 rounded-2xl bg-white p-5 ring-1 ring-line">
          <span className="flex size-10 shrink-0 items-center justify-center rounded-full bg-cobalt-50 text-cobalt-600">
            <Gavel className="size-5" aria-hidden />
          </span>
          <p className="text-ink-2">
            {auction.status === 'Scheduled'
              ? `No bids yet. Bidding opens at ${formatMoney(auction.startingPrice)}.`
              : isOpen(auction)
                ? `No bids yet. Be the first at ${formatMoney(auction.startingPrice)}.`
                : 'Nobody bid on this item.'}
          </p>
        </div>
      ) : (
        <>
          <div className="rounded-2xl bg-white p-4 ring-1 ring-line sm:p-5">
            <PriceHistory auction={auction} bids={history.data.items} complete={history.data.totalCount <= history.data.items.length} />
          </div>
          <BidLog key={auction.id} bids={history.data.items} totalCount={history.data.totalCount} />
        </>
      )}
    </section>
  )
}

function MissingAuction() {
  return (
    <EmptyState
      icon={<SearchX />}
      title="This auction does not exist"
      description="It may have been withdrawn, or the link is incomplete."
      action={
        <Button asChild>
          <Link to="/">Browse live auctions</Link>
        </Button>
      }
    />
  )
}

function AuctionSkeleton() {
  return (
    <div className="grid grid-cols-1 gap-8 lg:grid-cols-[minmax(0,1fr)_380px] lg:gap-10" aria-busy>
      <div className="grid content-start gap-5">
        <Skeleton className="h-5 w-28" />
        <Skeleton className="h-10 w-3/4" />
        <Skeleton className="aspect-[16/9] rounded-3xl" />
      </div>
      <Skeleton className="h-96 rounded-3xl" />
    </div>
  )
}
