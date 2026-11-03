import { ChevronLeft, ChevronRight } from 'lucide-react'
import { Button } from './Button'

type PaginationProps = { page: number; totalPages: number; onPageChange: (page: number) => void }

export function Pagination({ page, totalPages, onPageChange }: PaginationProps) {
  if (totalPages <= 1) return null

  return (
    <nav className="flex items-center justify-between gap-4 pt-8 text-sm text-muted" aria-label="Pagination">
      <Button variant="secondary" size="sm" onClick={() => onPageChange(page - 1)} disabled={page <= 1}>
        <ChevronLeft /> Previous
      </Button>
      <span>
        Page {page} of {totalPages}
      </span>
      <Button variant="secondary" size="sm" onClick={() => onPageChange(page + 1)} disabled={page >= totalPages}>
        Next <ChevronRight />
      </Button>
    </nav>
  )
}
