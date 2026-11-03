import { LotArt } from '@/components/LotArt'
import { categoryLabels } from '@/features/browse/categoryLabels'
import { formatDuration } from '@/features/rules/formatDuration'
import { type AuctionCategory, AUCTION_CATEGORIES } from '@/lib/api/types'
import { useAuth } from '@/lib/auth/AuthContext'
import { formatMoney } from '@/lib/format'

type ListingPreviewProps = { title: string; category: string; startingPrice: string; durationMinutes: string; hasReserve: boolean }

const PREVIEW_ID = 'listing-preview'

const asCategory = (value: string) => AUCTION_CATEGORIES.find((category) => category === value)

export function ListingPreview({ title, category, startingPrice, durationMinutes, hasReserve }: ListingPreviewProps) {
  const { user } = useAuth()
  const selected: AuctionCategory = asCategory(category) ?? 'Collectibles'
  const price = Number(startingPrice)
  const minutes = Number(durationMinutes)

  return (
    <figure className="overflow-hidden rounded-2xl bg-white ring-1 ring-line">
      <div className="relative">
        <LotArt id={PREVIEW_ID} category={selected} className="aspect-[4/3]" />
        <span className="absolute top-3 left-3 rounded-full bg-white/85 px-2.5 py-1 text-xs font-medium text-ink-2">
          {asCategory(category) ? categoryLabels[selected] : 'Pick a category'}
        </span>
        {minutes > 0 && (
          <span className="absolute top-3 right-3 rounded-full bg-white px-2.5 py-1 text-xs">Runs {formatDuration(minutes)}</span>
        )}
      </div>
      <figcaption className="p-4">
        <p className={title.trim() ? 'line-clamp-2 leading-snug font-medium' : 'leading-snug text-muted'}>
          {title.trim() || 'Your title appears here'}
        </p>
        <p className="mt-0.5 text-sm text-muted">by {user?.displayName}</p>
        <div className="mt-4 flex items-end justify-between gap-3">
          <div>
            <p className="text-xs text-muted">Starting bid</p>
            <p className="tabular font-display text-2xl font-semibold">{formatMoney(price > 0 ? price : 0)}</p>
          </div>
          {hasReserve && <p className="text-xs text-muted">Reserve not met</p>}
        </div>
      </figcaption>
    </figure>
  )
}
