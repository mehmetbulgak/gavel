export function FormError({ message }: { message?: string | null }) {
  if (!message) return null

  return (
    <div role="alert" className="rounded-xl bg-flare-50 px-4 py-3 text-sm text-flare-700">
      {message}
    </div>
  )
}
