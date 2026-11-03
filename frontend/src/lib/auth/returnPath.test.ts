import { describe, expect, it } from 'vitest'
import { safeReturnPath } from './returnPath'

describe('safeReturnPath', () => {
  it.each(['/auctions/9b2f', '/activity?tab=won'])('keeps the local path %s', (path) => {
    expect(safeReturnPath(path)).toBe(path)
  })

  it.each([null, '', 'https://evil.example', '//evil.example', '/\\evil.example', 'javascript:alert(1)', 'auctions'])(
    'falls back to home for %s',
    (path) => {
      expect(safeReturnPath(path)).toBe('/')
    },
  )
})
