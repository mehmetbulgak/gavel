import { useId, type ReactNode } from 'react'
import { cn } from '@/lib/cn'
import { Label } from './FormControls'

type ControlProps = { id: string; 'aria-invalid'?: true; 'aria-describedby'?: string }

type FieldProps = {
  label: string
  error?: string
  hint?: ReactNode
  className?: string
  children: (props: ControlProps) => ReactNode
}

export function Field({ label, error, hint, className, children }: FieldProps) {
  const id = useId()
  const messageId = `${id}-message`
  const hasMessage = Boolean(error || hint)

  return (
    <div className={cn('grid content-start gap-1.5', className)}>
      <Label htmlFor={id}>{label}</Label>
      {children({
        id,
        ...(error ? { 'aria-invalid': true as const } : {}),
        ...(hasMessage ? { 'aria-describedby': messageId } : {}),
      })}
      {error ? (
        <p id={messageId} className="text-sm text-flare-700">
          {error}
        </p>
      ) : hint ? (
        <p id={messageId} className="text-sm text-muted">
          {hint}
        </p>
      ) : null}
    </div>
  )
}
