import { useMemo, useState, type PointerEvent } from 'react'
import { Paddle } from '@/components/Paddle'
import type { AuctionDetails, Bid } from '@/lib/api/types'
import { niceTicks } from '@/lib/chart'
import { formatClock, formatDateTime, formatMoney, formatShortTime, pluralize } from '@/lib/format'
import { useElementWidth } from '@/lib/hooks/useElementWidth'
import { useServerNow } from '@/lib/time/useNow'
import { buildPriceSeries, pointAt } from './priceSeries'

const HEIGHT = 220
const MARGIN = { top: 14, right: 16, bottom: 30 }
const LONG_SPAN_MS = 20 * 60 * 60 * 1000
const TICK_GAP = 10

type PriceHistoryProps = { auction: AuctionDetails; bids: readonly Bid[]; complete: boolean }

export function PriceHistory({ auction, bids, complete }: PriceHistoryProps) {
  const { ref, width } = useElementWidth<HTMLDivElement>()
  const now = useServerNow(5000)
  const [active, setActive] = useState<number | null>(null)

  const points = useMemo(
    () => buildPriceSeries({ startingPrice: auction.startingPrice, startsAt: auction.startsAt, bids, complete }),
    [auction.startingPrice, auction.startsAt, bids, complete],
  )

  const first = points[0]
  const last = points.at(-1)
  if (!first || !last) return null

  const tail = Math.max(last.time, auction.status === 'Live' ? now : Date.parse(auction.endsAt))
  const ticks = niceTicks(Math.min(...points.map((p) => p.price)), last.price)
  const yMin = ticks[0] ?? 0
  const yMax = ticks.at(-1) ?? last.price
  const left = Math.max(...ticks.map((tick) => formatMoney(tick).length)) * 7 + TICK_GAP + 6
  const plotWidth = Math.max(0, width - left - MARGIN.right)
  const plotHeight = HEIGHT - MARGIN.top - MARGIN.bottom
  const span = Math.max(1, tail - first.time)

  const x = (time: number) => left + ((time - first.time) / span) * plotWidth
  const y = (price: number) => MARGIN.top + (1 - (price - yMin) / Math.max(1, yMax - yMin)) * plotHeight

  const line =
    points.reduce(
      (path, point, index) => (index === 0 ? `M${x(point.time)},${y(point.price)}` : `${path}H${x(point.time)}V${y(point.price)}`),
      '',
    ) + `H${x(tail)}`
  const area = `${line}V${y(yMin)}H${x(first.time)}Z`
  const timeLabel = (time: number) => (span > LONG_SPAN_MS ? formatDateTime(new Date(time).toISOString()) : formatShortTime(time))

  const onPointerMove = (event: PointerEvent<SVGRectElement>) => {
    const bounds = event.currentTarget.getBoundingClientRect()
    const time = first.time + ((event.clientX - bounds.left) / bounds.width) * span
    setActive(pointAt(points, time))
  }

  const hovered = active === null ? null : points[active]
  const summary = `Price rose from ${formatMoney(first.price)} to ${formatMoney(last.price)} across ${pluralize(bids.length, 'bid')}.`

  return (
    <div ref={ref} className="relative w-full" style={{ height: HEIGHT }}>
      {width > 0 && (
        <svg width={width} height={HEIGHT} role="img" aria-label={summary} className="absolute inset-0 overflow-visible">
          {ticks.map((tick) => (
            <g key={tick}>
              <line x1={left} x2={width - MARGIN.right} y1={y(tick)} y2={y(tick)} className="stroke-line" strokeWidth={1} />
              <text x={left - TICK_GAP} y={y(tick)} dy="0.32em" textAnchor="end" className="tabular fill-muted text-xs">
                {formatMoney(tick)}
              </text>
            </g>
          ))}
          <text x={left} y={HEIGHT - 8} className="fill-muted text-xs">
            {timeLabel(first.time)}
          </text>
          <text x={width - MARGIN.right} y={HEIGHT - 8} textAnchor="end" className="fill-muted text-xs">
            {auction.status === 'Live' ? 'Now' : timeLabel(tail)}
          </text>
          <path d={area} className="fill-cobalt-500/8" />
          <path d={line} fill="none" className="stroke-cobalt-500" strokeWidth={2} strokeLinejoin="round" />
          {hovered && (
            <line x1={x(hovered.time)} x2={x(hovered.time)} y1={MARGIN.top} y2={y(yMin)} className="stroke-ink/25" strokeWidth={1} />
          )}
          <circle
            cx={x((hovered ?? last).time)}
            cy={y((hovered ?? last).price)}
            r={5}
            className="fill-cobalt-500 stroke-white"
            strokeWidth={2}
          />
          <rect
            x={left}
            y={0}
            width={plotWidth}
            height={HEIGHT - MARGIN.bottom}
            fill="transparent"
            onPointerMove={onPointerMove}
            onPointerLeave={() => setActive(null)}
          />
        </svg>
      )}
      {hovered && (
        <div
          className="pointer-events-none absolute z-10 w-max -translate-x-1/2 -translate-y-full rounded-xl bg-ink px-3 py-2 text-sm text-white shadow-lg"
          style={{
            left: Math.min(Math.max(x(hovered.time), 90), width - 90),
            top: y(hovered.price) - 12,
          }}
        >
          <p className="tabular font-display text-base font-semibold">{formatMoney(hovered.price)}</p>
          {hovered.bid ? (
            <p className="flex items-center gap-2 text-white/75">
              <Paddle alias={hovered.bid.alias} isYou={hovered.bid.isMine} tone="board" />
              <span className="tabular">{formatClock(hovered.bid.placedAt)}</span>
            </p>
          ) : (
            <p className="text-white/75">Starting price</p>
          )}
        </div>
      )}
    </div>
  )
}
