import type { ReactNode } from 'react'

type EmptyStateProps = { icon: ReactNode; title: string; description?: string; action?: ReactNode }

export function EmptyState({ icon, title, description, action }: EmptyStateProps) {
  return (
    <div className="flex flex-col items-center px-6 py-16 text-center">
      <div className="mb-4 flex size-12 items-center justify-center rounded-full bg-white text-muted ring-1 ring-line [&_svg]:size-5">
        {icon}
      </div>
      <p className="font-display text-lg font-semibold">{title}</p>
      {description && <p className="mt-1 max-w-sm text-muted">{description}</p>}
      {action && <div className="mt-5">{action}</div>}
    </div>
  )
}
