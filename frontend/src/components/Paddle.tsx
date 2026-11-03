import { cn } from '@/lib/cn'
import { paddleNumber } from '@/lib/format'

type PaddleProps = { alias: string | null | undefined; isYou?: boolean; className?: string; tone?: 'light' | 'board' }

export function Paddle({ alias, isYou = false, className, tone = 'light' }: PaddleProps) {
  const number = paddleNumber(alias)
  if (number === null) return null

  return (
    <span className={cn('inline-flex items-center gap-1.5 text-sm font-medium', className)}>
      <svg viewBox="0 0 20 24" className="h-5 w-4 shrink-0" aria-hidden>
        <ellipse cx="10" cy="8.5" rx="8.5" ry="8" className={isYou ? 'fill-mint-500' : tone === 'board' ? 'fill-white' : 'fill-ink'} />
        <rect x="8.5" y="15" width="3" height="8.5" rx="1.5" className={tone === 'board' ? 'fill-white/70' : 'fill-ink-2'} />
        <text
          x="10"
          y="11.5"
          textAnchor="middle"
          className={cn('font-sans text-[9px] font-bold', isYou ? 'fill-white' : tone === 'board' ? 'fill-ink' : 'fill-white')}
        >
          {number}
        </text>
      </svg>
      <span>{isYou ? 'You' : `Paddle ${number}`}</span>
    </span>
  )
}
