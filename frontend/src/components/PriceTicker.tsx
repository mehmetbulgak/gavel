import { cn } from '@/lib/cn'
import { formatMoney } from '@/lib/format'

const DIGITS = ['0', '1', '2', '3', '4', '5', '6', '7', '8', '9']

type PriceTickerProps = { value: number; className?: string; label?: string }

export function PriceTicker({ value, className, label }: PriceTickerProps) {
  const text = formatMoney(value)
  const characters = [...text]

  return (
    <span className={cn('tabular inline-flex items-start leading-none', className)}>
      <span className="sr-only">{label ? `${label}: ${text}` : text}</span>
      {characters.map((character, index) => {
        const key = characters.length - index
        if (!/\d/.test(character)) {
          return (
            <span key={`s-${key}`} className="inline-block h-[1em]" aria-hidden>
              {character}
            </span>
          )
        }

        return (
          <span key={`d-${key}`} className="relative inline-block h-[1em] w-[0.62em] overflow-hidden" aria-hidden>
            <span
              className="absolute inset-x-0 top-0 flex flex-col transition-transform duration-500 ease-out"
              style={{ transform: `translateY(-${Number(character)}em)` }}
            >
              {DIGITS.map((digit) => (
                <span key={digit} className="block h-[1em] text-center">
                  {digit}
                </span>
              ))}
            </span>
          </span>
        )
      })}
    </span>
  )
}
