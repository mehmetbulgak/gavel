import { Search } from 'lucide-react'
import { Input, Select } from '@/components/ui/FormControls'
import { AUCTION_CATEGORIES, type AuctionCategory } from '@/lib/api/types'
import { cn } from '@/lib/cn'
import type { AuctionSort } from './api'
import { categoryLabels } from './categoryLabels'

const sorts: { value: AuctionSort; label: string }[] = [
  { value: 'EndingSoon', label: 'Ending soonest' },
  { value: 'Newest', label: 'Newly listed' },
  { value: 'MostBids', label: 'Most bids' },
  { value: 'PriceLow', label: 'Lowest price' },
  { value: 'PriceHigh', label: 'Highest price' },
]

type BrowseFiltersProps = {
  category: AuctionCategory | undefined
  sort: AuctionSort
  search: string
  onCategory: (category: AuctionCategory | undefined) => void
  onSort: (sort: AuctionSort) => void
  onSearch: (search: string) => void
}

export function BrowseFilters({ category, sort, search, onCategory, onSort, onSearch }: BrowseFiltersProps) {
  const chip = (active: boolean) =>
    cn(
      'shrink-0 cursor-pointer rounded-full px-3.5 py-1.5 text-sm font-medium ring-1 transition-colors',
      active ? 'bg-ink text-white ring-ink' : 'bg-white text-ink-2 ring-line hover:ring-ink/30',
    )

  return (
    <div className="grid gap-4">
      <div className="flex flex-wrap items-center gap-3">
        <div className="relative min-w-56 flex-1">
          <Search className="pointer-events-none absolute top-1/2 left-3.5 size-4 -translate-y-1/2 text-muted" aria-hidden />
          <Input
            type="search"
            aria-label="Search auctions"
            placeholder="Search by title"
            maxLength={100}
            className="pl-10"
            value={search}
            onChange={(e) => onSearch(e.target.value)}
          />
        </div>
        <Select aria-label="Sort auctions" className="w-48" value={sort} onChange={(e) => onSort(e.target.value as AuctionSort)}>
          {sorts.map((option) => (
            <option key={option.value} value={option.value}>
              {option.label}
            </option>
          ))}
        </Select>
      </div>
      <div role="group" aria-label="Category" className="-mx-1 flex gap-2 overflow-x-auto px-1 pb-1">
        <button type="button" aria-pressed={!category} className={chip(!category)} onClick={() => onCategory(undefined)}>
          Everything
        </button>
        {AUCTION_CATEGORIES.map((value) => (
          <button
            key={value}
            type="button"
            aria-pressed={category === value}
            className={chip(category === value)}
            onClick={() => onCategory(value)}
          >
            {categoryLabels[value]}
          </button>
        ))}
      </div>
    </div>
  )
}
