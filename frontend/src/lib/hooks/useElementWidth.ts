import { useEffect, useState } from 'react'

export function useElementWidth<T extends HTMLElement>() {
  const [element, setElement] = useState<T | null>(null)
  const [width, setWidth] = useState(0)

  useEffect(() => {
    if (!element) return undefined

    const observer = new ResizeObserver(([entry]) => setWidth(entry?.contentRect.width ?? 0))
    observer.observe(element)
    return () => observer.disconnect()
  }, [element])

  return { ref: setElement, width }
}
