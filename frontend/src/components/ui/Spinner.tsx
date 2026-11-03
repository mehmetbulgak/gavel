import { LoaderCircle } from 'lucide-react'
import { cn } from '@/lib/cn'

export function Spinner({ fullPage = false }: { fullPage?: boolean }) {
  return (
    <div role="status" className={cn('flex items-center justify-center text-muted', fullPage ? 'min-h-dvh' : 'py-24')}>
      <LoaderCircle className="size-6 animate-spin" aria-hidden />
      <span className="sr-only">Loading</span>
    </div>
  )
}
