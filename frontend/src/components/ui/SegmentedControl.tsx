import { cn } from '@/lib/cn'

type Option<T extends string> = { value: T; label: string }

type SegmentedControlProps<T extends string> = {
  label: string
  value: T
  options: readonly Option<T>[]
  onChange: (value: T) => void
  className?: string
}

export function SegmentedControl<T extends string>({ label, value, options, onChange, className }: SegmentedControlProps<T>) {
  return (
    <div
      role="group"
      aria-label={label}
      className={cn('inline-flex flex-wrap gap-1 rounded-full bg-white p-1 ring-1 ring-line', className)}
    >
      {options.map((option) => (
        <button
          key={option.value}
          type="button"
          aria-pressed={value === option.value}
          onClick={() => onChange(option.value)}
          className={cn(
            'cursor-pointer rounded-full px-3.5 py-1.5 text-sm font-medium transition-colors',
            value === option.value ? 'bg-ink text-white' : 'text-ink-2 hover:bg-ink/5',
          )}
        >
          {option.label}
        </button>
      ))}
    </div>
  )
}
