import { Compass } from 'lucide-react'
import { Link } from 'react-router'
import { Button } from './ui/Button'
import { EmptyState } from './ui/EmptyState'

export function NotFoundPage() {
  return (
    <EmptyState
      icon={<Compass />}
      title="There is nothing at this address"
      description="The link may be mistyped, or the page has moved."
      action={
        <Button asChild>
          <Link to="/">Go to live auctions</Link>
        </Button>
      }
    />
  )
}
