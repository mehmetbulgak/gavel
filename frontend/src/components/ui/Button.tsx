import { Slot } from '@radix-ui/react-slot'
import { cva, type VariantProps } from 'class-variance-authority'
import { LoaderCircle } from 'lucide-react'
import type { ComponentProps } from 'react'
import { cn } from '@/lib/cn'

const buttonVariants = cva(
  'inline-flex shrink-0 cursor-pointer items-center justify-center gap-2 rounded-full font-medium whitespace-nowrap transition-colors disabled:pointer-events-none disabled:opacity-50 [&_svg]:size-4 [&_svg]:shrink-0',
  {
    variants: {
      variant: {
        primary: 'bg-cobalt-500 text-white hover:bg-cobalt-600',
        secondary: 'border border-line bg-white text-ink hover:border-ink-2/40',
        ghost: 'text-ink-2 hover:bg-ink/5 hover:text-ink',
        board: 'bg-white text-ink hover:bg-cobalt-50',
        danger: 'border border-flare-300 bg-white text-flare-700 hover:bg-flare-50',
      },
      size: {
        sm: 'h-8 px-3.5 text-sm',
        md: 'h-10 px-5 text-sm',
        lg: 'h-12 px-6 text-base',
        icon: 'size-10',
      },
    },
    defaultVariants: { variant: 'primary', size: 'md' },
  },
)

type ButtonProps = ComponentProps<'button'> & VariantProps<typeof buttonVariants> & { asChild?: boolean; loading?: boolean }

export function Button({ className, variant, size, asChild, loading, disabled, children, ...props }: ButtonProps) {
  if (asChild) {
    return (
      <Slot className={cn(buttonVariants({ variant, size }), className)} {...props}>
        {children}
      </Slot>
    )
  }

  return (
    <button className={cn(buttonVariants({ variant, size }), className)} disabled={disabled || loading} {...props}>
      {loading && <LoaderCircle className="animate-spin" aria-hidden />}
      {children}
    </button>
  )
}
