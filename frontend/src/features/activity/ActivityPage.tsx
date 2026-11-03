import { AlertTriangle, Bookmark, Gavel, PackageOpen, Trophy } from 'lucide-react'
import type { ReactNode } from 'react'
import { Link } from 'react-router'
import { Button } from '@/components/ui/Button'
import { EmptyState } from '@/components/ui/EmptyState'
import { Pagination } from '@/components/ui/Pagination'
import { SegmentedControl } from '@/components/ui/SegmentedControl'
import { Skeleton } from '@/components/ui/Skeleton'
import type { AuctionSummary, MyBid, Paged } from '@/lib/api/types'
import { formatMoney } from '@/lib/format'
import { useUrlFilters } from '@/lib/hooks/useUrlFilters'
import { type ActivityTab, useMyAuctions, useMyBids } from './api'
import { ActivityRow } from './ActivityRow'
import { sellerBadge } from './sellerBadge'
import { StatusBadge } from './StatusBadge'

const TABS = [
  { value: 'bidding', label: 'Bidding' },
  { value: 'won', label: 'Won' },
  { value: 'lost', label: 'Lost' },
  { value: 'selling', label: 'Selling' },
  { value: 'watching', label: 'Watching' },
] as const satisfies readonly { value: ActivityTab; label: string }[]

type Empty = { icon: ReactNode; title: string; description: string; action?: { to: string; label: string } }

const empty: Record<ActivityTab, Empty> = {
  bidding: {
    icon: <Gavel />,
    title: 'You are not bidding on anything',
    description: 'Live auctions you bid on show up here, with whether you are still in the lead.',
    action: { to: '/', label: 'Browse live auctions' },
  },
  won: { icon: <Trophy />, title: 'No wins yet', description: 'Auctions you win land here once they close.' },
  lost: { icon: <Gavel />, title: 'Nothing lost so far', description: 'Closed auctions where someone outbid you are listed here.' },
  selling: {
    icon: <PackageOpen />,
    title: 'You have not listed anything',
    description: 'Listings you create, live or finished, appear here.',
    action: { to: '/sell', label: 'Sell an item' },
  },
  watching: {
    icon: <Bookmark />,
    title: 'Your watchlist is empty',
    description: 'Watch an auction to get a heads-up before it closes.',
    action: { to: '/', label: 'Find something to watch' },
  },
}

const isBidTab = (tab: ActivityTab) => tab === 'bidding' || tab === 'won' || tab === 'lost'

export default function ActivityPage() {
  const { params, update, page } = useUrlFilters()
  const tab = TABS.find((t) => t.value === params.get('tab'))?.value ?? 'bidding'
  const bids = useMyBids(tab, page)
  const auctions = useMyAuctions(tab, page)
  const query = isBidTab(tab) ? bids : auctions

  return (
    <div className="grid grid-cols-1 gap-6">
      <title>My activity | Gavel</title>
      <div>
        <h1 className="text-3xl font-bold tracking-tight">My activity</h1>
        <p className="mt-1 text-muted">Everything you are bidding on, selling and keeping an eye on.</p>
      </div>
      <SegmentedControl
        label="Activity"
        value={tab}
        options={TABS}
        onChange={(value) => update({ tab: value === 'bidding' ? null : value })}
        className="w-fit"
      />

      {query.isError ? (
        <EmptyState icon={<AlertTriangle />} title="Your activity could not be loaded" description="Refresh the page to try again." />
      ) : query.isPending ? (
        <div className="grid gap-3">
          {Array.from({ length: 4 }, (_, i) => (
            <Skeleton key={i} className="h-24 rounded-2xl" />
          ))}
        </div>
      ) : isBidTab(tab) && bids.data ? (
        <ActivityList page={bids.data} tab={tab} onPage={(next) => update({ page: String(next) })}>
          {(bid: MyBid) => (
            <ActivityRow
              auction={bid.auction}
              badge={<BidBadge bid={bid} tab={tab} />}
              detail={<span>Your max {formatMoney(bid.maxBid)}</span>}
            />
          )}
        </ActivityList>
      ) : auctions.data ? (
        <ActivityList page={auctions.data} tab={tab} onPage={(next) => update({ page: String(next) })}>
          {(auction: AuctionSummary) => (
            <ActivityRow auction={auction} badge={tab === 'selling' ? <SellerBadge auction={auction} /> : undefined} />
          )}
        </ActivityList>
      ) : null}
    </div>
  )
}

type ActivityListProps<T> = { page: Paged<T>; tab: ActivityTab; onPage: (page: number) => void; children: (item: T) => ReactNode }

function ActivityList<T extends MyBid | AuctionSummary>({ page, tab, onPage, children }: ActivityListProps<T>) {
  if (page.items.length === 0) {
    const { icon, title, description, action } = empty[tab]
    return (
      <EmptyState
        icon={icon}
        title={title}
        description={description}
        action={
          action && (
            <Button asChild>
              <Link to={action.to}>{action.label}</Link>
            </Button>
          )
        }
      />
    )
  }

  return (
    <div>
      <ul className="grid grid-cols-1 gap-3">
        {page.items.map((item) => (
          <li key={'auction' in item ? item.auction.id : item.id}>{children(item)}</li>
        ))}
      </ul>
      <Pagination page={page.page} totalPages={page.totalPages} onPageChange={onPage} />
    </div>
  )
}

function BidBadge({ bid, tab }: { bid: MyBid; tab: ActivityTab }) {
  if (tab === 'won') return <StatusBadge tone="good">Won</StatusBadge>
  if (tab === 'lost') return <StatusBadge tone="neutral">{bid.auction.outcome === 'Sold' ? 'Outbid' : 'Not sold'}</StatusBadge>
  return bid.isLeading ? <StatusBadge tone="good">Winning</StatusBadge> : <StatusBadge tone="bad">Outbid</StatusBadge>
}

function SellerBadge({ auction }: { auction: AuctionSummary }) {
  const { tone, label } = sellerBadge(auction)
  return <StatusBadge tone={tone}>{label}</StatusBadge>
}
