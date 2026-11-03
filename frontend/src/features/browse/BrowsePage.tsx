import { AlertTriangle, PackageSearch } from 'lucide-react'
import { useState } from 'react'
import { Link } from 'react-router'
import { Button } from '@/components/ui/Button'
import { EmptyState } from '@/components/ui/EmptyState'
import { Pagination } from '@/components/ui/Pagination'
import { Skeleton } from '@/components/ui/Skeleton'
import { AUCTION_CATEGORIES, type AuctionCategory } from '@/lib/api/types'
import { useAuth } from '@/lib/auth/AuthContext'
import { useDebouncedValue } from '@/lib/hooks/useDebouncedValue'
import { useUrlFilters } from '@/lib/hooks/useUrlFilters'
import { useLiveLobby } from '@/lib/realtime/AuctionLiveContext'
import { type AuctionSort, type ListStatus, useAuctions, useMarketStats } from './api'
import { AuctionCard } from './AuctionCard'
import { BrowseFilters } from './BrowseFilters'
import { OnTheBlock } from './OnTheBlock'

const PAGE_SIZE = 12
const STATUSES: readonly ListStatus[] = ['Live', 'Upcoming', 'Ended']
const SORTS: readonly AuctionSort[] = ['EndingSoon', 'Newest', 'MostBids', 'PriceLow', 'PriceHigh']

const headings: Record<ListStatus, { title: string; description: string }> = {
  Live: { title: 'Live auctions', description: 'Bids land in real time. Prices on this page move as people bid.' },
  Upcoming: { title: 'Starting soon', description: 'Watch an item now and you will hear when it is about to close.' },
  Ended: { title: 'Results', description: 'Recently closed auctions and what they sold for.' },
}

const pick = <T extends string>(allowed: readonly T[], value: string | null, fallback: T) =>
  allowed.find((item) => item === value) ?? fallback

export default function BrowsePage() {
  useLiveLobby()
  const { status: authStatus } = useAuth()
  const { params, update, page } = useUrlFilters()
  const [search, setSearch] = useState(params.get('search') ?? '')
  const debouncedSearch = useDebouncedValue(search.trim())

  const status = pick(STATUSES, params.get('status'), 'Live')
  const category = AUCTION_CATEGORIES.find((c) => c === params.get('category')) as AuctionCategory | undefined
  const sort = pick(SORTS, params.get('sort'), 'EndingSoon')
  const filtered = Boolean(category || debouncedSearch)

  const auctions = useAuctions({ status, category, sort, search: debouncedSearch || undefined, page, pageSize: PAGE_SIZE })
  const closingNext = useAuctions({ status: 'Live', sort: 'EndingSoon', pageSize: 1 })
  const stats = useMarketStats()
  const featured = status === 'Live' && !filtered && page === 1 ? closingNext.data?.items[0] : undefined
  const heading = headings[status]

  return (
    <div className="grid grid-cols-1 gap-10">
      {featured && <OnTheBlock auction={featured} stats={stats.data} />}

      <section aria-labelledby="listing-heading" className="grid grid-cols-1 gap-6">
        <div>
          <h1 id="listing-heading" className="text-3xl font-bold tracking-tight">
            {heading.title}
          </h1>
          <p className="mt-1 text-muted">{heading.description}</p>
        </div>

        <BrowseFilters
          category={category}
          sort={sort}
          search={search}
          onCategory={(value) => update({ category: value ?? null })}
          onSort={(value) => update({ sort: value === 'EndingSoon' ? null : value })}
          onSearch={(value) => {
            setSearch(value)
            update({ search: value || null })
          }}
        />

        {auctions.isError ? (
          <EmptyState
            icon={<AlertTriangle />}
            title="Auctions could not be loaded"
            description="Check your connection; the list refreshes on its own."
          />
        ) : auctions.isPending ? (
          <div className="grid gap-5 sm:grid-cols-2 lg:grid-cols-3">
            {Array.from({ length: 6 }, (_, i) => (
              <Skeleton key={i} className="h-80 rounded-2xl" />
            ))}
          </div>
        ) : auctions.data.items.length === 0 ? (
          <EmptyState
            icon={<PackageSearch />}
            title={filtered ? 'Nothing matches these filters' : 'No auctions here yet'}
            description={filtered ? 'Try another category or a shorter search.' : 'List something and it will show up for everyone.'}
            action={
              authStatus === 'authenticated' && !filtered ? (
                <Button asChild>
                  <Link to="/sell">Sell an item</Link>
                </Button>
              ) : undefined
            }
          />
        ) : (
          <div>
            <ul className="grid gap-5 sm:grid-cols-2 lg:grid-cols-3">
              {auctions.data.items.map((auction) => (
                <li key={auction.id} className="flex">
                  <AuctionCard auction={auction} />
                </li>
              ))}
            </ul>
            <Pagination
              page={auctions.data.page}
              totalPages={auctions.data.totalPages}
              onPageChange={(next) => update({ page: String(next) })}
            />
          </div>
        )}
      </section>
    </div>
  )
}
