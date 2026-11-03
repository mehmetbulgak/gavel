import type { ComponentProps } from 'react'
import { cn } from '@/lib/cn'

const control =
  'w-full rounded-xl border border-line bg-white px-3.5 text-base text-ink transition-colors placeholder:text-muted/70 focus:border-cobalt-500 focus:outline-none focus:ring-4 focus:ring-cobalt-500/15 disabled:bg-paper disabled:text-muted aria-invalid:border-flare-500 aria-invalid:focus:ring-flare-500/15'

export function Input({ className, ...props }: ComponentProps<'input'>) {
  return <input className={cn(control, 'h-11', className)} {...props} />
}

export function Textarea({ className, ...props }: ComponentProps<'textarea'>) {
  return <textarea className={cn(control, 'min-h-28 py-2.5', className)} {...props} />
}

export function Select({ className, ...props }: ComponentProps<'select'>) {
  return <select className={cn(control, 'h-11 pr-9', className)} {...props} />
}

export function Label({ className, ...props }: ComponentProps<'label'>) {
  return <label className={cn('text-sm font-medium text-ink', className)} {...props} />
}
