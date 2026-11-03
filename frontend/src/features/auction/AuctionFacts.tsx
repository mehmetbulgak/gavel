import type { AuctionDetails } from '@/lib/api/types'
import { formatDateTime, formatMoney, formatNumber } from '@/lib/format'

function reserveText(auction: AuctionDetails) {
  if (!auction.hasReserve) return 'No reserve'
  if (auction.reservePrice !== null) return `${formatMoney(auction.reservePrice)}, visible only to you`
  return auction.reserveMet ? 'Met' : 'Not met yet'
}

export function AuctionFacts({ auction }: { auction: AuctionDetails }) {
  const opened = auction.status !== 'Scheduled'
  const closed = auction.status === 'Ended' || auction.status === 'Cancelled'
  const facts = [
    { term: 'Starting price', value: formatMoney(auction.startingPrice) },
    { term: 'Bid increment', value: formatMoney(auction.increment) },
    { term: 'Reserve', value: reserveText(auction) },
    { term: 'Bidders', value: formatNumber(auction.bidderCount) },
    { term: opened ? 'Opened' : 'Opens', value: formatDateTime(auction.startsAt) },
    { term: closed ? 'Closed' : 'Closes', value: formatDateTime(auction.endsAt) },
  ]

  return (
    <section aria-labelledby="facts-heading" className="rounded-3xl bg-white p-6 ring-1 ring-line">
      <h2 id="facts-heading" className="font-display text-lg font-semibold">
        Lot details
      </h2>
      <dl className="mt-4 grid grid-cols-2 gap-x-4 gap-y-4">
        {facts.map((fact) => (
          <div key={fact.term}>
            <dt className="text-sm text-muted">{fact.term}</dt>
            <dd className="tabular mt-0.5 font-medium">{fact.value}</dd>
          </div>
        ))}
      </dl>
    </section>
  )
}
