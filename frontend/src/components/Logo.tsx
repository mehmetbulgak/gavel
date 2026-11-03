import { cn } from '@/lib/cn'

export function Logo({ className, inverted = false }: { className?: string; inverted?: boolean }) {
  return (
    <span className={cn('inline-flex items-center gap-2', className)}>
      <svg viewBox="0 0 32 32" className="size-8" aria-hidden>
        <rect width="32" height="32" rx="9" className={inverted ? 'fill-white' : 'fill-ink'} />
        <g transform="rotate(-40 16 16)">
          <rect x="9" y="7" width="14" height="8" rx="2.5" className="fill-cobalt-500" />
          <rect x="14.75" y="14" width="2.5" height="12" rx="1.25" className={inverted ? 'fill-ink' : 'fill-paper'} />
        </g>
        <rect x="7" y="24" width="12" height="2.5" rx="1.25" className="fill-flare-500" />
      </svg>
      <span className={cn('font-display text-xl font-bold tracking-tight', inverted ? 'text-white' : 'text-ink')}>Gavel</span>
    </span>
  )
}
