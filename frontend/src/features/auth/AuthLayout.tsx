import type { ReactNode } from 'react'
import { Link } from 'react-router'
import { Logo } from '@/components/Logo'

type AuthLayoutProps = { title: string; description: string; children: ReactNode; footer: ReactNode }

export function AuthLayout({ title, description, children, footer }: AuthLayoutProps) {
  return (
    <div className="grid min-h-dvh lg:grid-cols-[1fr_1.1fr]">
      <div className="flex flex-col px-6 py-8 sm:px-12">
        <Link to="/" className="self-start rounded-lg" aria-label="Back to live auctions">
          <Logo />
        </Link>
        <div className="mx-auto flex w-full max-w-sm flex-1 flex-col justify-center py-12">
          <h1 className="text-3xl font-bold tracking-tight">{title}</h1>
          <p className="mt-2 text-muted">{description}</p>
          <div className="mt-8">{children}</div>
          <p className="mt-8 text-sm text-muted">{footer}</p>
        </div>
      </div>
      <aside className="relative hidden overflow-hidden bg-board text-white lg:flex lg:flex-col lg:justify-end lg:p-14" aria-hidden>
        <svg className="absolute -top-24 -right-24 size-[34rem] text-board-2" viewBox="0 0 200 200">
          <g transform="rotate(-40 100 100)">
            <rect x="45" y="30" width="110" height="60" rx="16" fill="currentColor" />
            <rect x="90" y="85" width="20" height="100" rx="10" fill="currentColor" />
          </g>
        </svg>
        <p className="relative max-w-md font-display text-4xl leading-tight font-semibold">
          Set your maximum once. Gavel bids for you, one step at a time, and only as high as it needs to.
        </p>
        <p className="relative mt-6 max-w-md text-white/70">
          Late bids add two minutes to the clock, so the auction ends when bidding stops, not when someone snipes it.
        </p>
      </aside>
    </div>
  )
}
