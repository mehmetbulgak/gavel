import { cva, type VariantProps } from 'class-variance-authority'

const badge = cva('inline-flex items-center rounded-full px-2.5 py-0.5 text-xs font-semibold', {
  variants: {
    tone: {
      good: 'bg-mint-50 text-mint-700',
      bad: 'bg-flare-50 text-flare-700',
      neutral: 'bg-paper text-ink-2 ring-1 ring-line',
      info: 'bg-cobalt-50 text-cobalt-700',
    },
  },
  defaultVariants: { tone: 'neutral' },
})

type StatusBadgeProps = VariantProps<typeof badge> & { children: string }

export function StatusBadge({ tone, children }: StatusBadgeProps) {
  return <span className={badge({ tone })}>{children}</span>
}
