const FALLBACK = '/'

export function safeReturnPath(value: string | null | undefined) {
  if (!value || !value.startsWith('/') || value.startsWith('//') || value.startsWith('/\\')) {
    return FALLBACK
  }

  try {
    const url = new URL(value, window.location.origin)
    return url.origin === window.location.origin ? `${url.pathname}${url.search}` : FALLBACK
  } catch {
    return FALLBACK
  }
}
