import { RotateCw } from 'lucide-react'
import { Logo } from './Logo'
import { Button } from './ui/Button'

export function RouteError() {
  return (
    <div className="flex min-h-dvh flex-col items-center justify-center gap-6 px-6 text-center">
      <Logo />
      <div>
        <h1 className="text-2xl font-bold">This page stopped working</h1>
        <p className="mt-2 max-w-sm text-muted">Something unexpected happened while showing it. Reloading usually fixes it.</p>
      </div>
      <Button onClick={() => window.location.reload()}>
        <RotateCw /> Reload the page
      </Button>
    </div>
  )
}
