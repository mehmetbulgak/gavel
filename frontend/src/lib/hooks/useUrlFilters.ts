import { useCallback } from 'react'
import { useSearchParams } from 'react-router'

export type FilterChanges = Record<string, string | null>

const PAGE_KEY = 'page'

export function useUrlFilters() {
  const [params, setParams] = useSearchParams()

  const update = useCallback(
    (changes: FilterChanges) =>
      setParams((current) => {
        const next = new URLSearchParams(current)
        for (const [key, value] of Object.entries(changes)) {
          if (value === null) next.delete(key)
          else next.set(key, value)
        }
        if (!(PAGE_KEY in changes)) next.delete(PAGE_KEY)
        return next
      }),
    [setParams],
  )

  const page = Math.max(1, Number(params.get(PAGE_KEY) ?? 1) || 1)

  return { params, update, page }
}
