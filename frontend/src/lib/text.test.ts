import { describe, expect, it } from 'vitest'
import { hasControlCharacters } from './text'

describe('hasControlCharacters', () => {
  it('accepts ordinary text in any script', () => {
    expect(hasControlCharacters('Çok temiz Leica M6, kutulu')).toBe(false)
  })

  it('rejects control characters', () => {
    expect(hasControlCharacters('Title\u0000')).toBe(true)
    expect(hasControlCharacters('Title\u0085')).toBe(true)
    expect(hasControlCharacters('Line one\nLine two')).toBe(true)
  })

  it('allows line breaks and tabs only when asked to', () => {
    expect(hasControlCharacters('Line one\r\n\tLine two', { allowLineBreaks: true })).toBe(false)
    expect(hasControlCharacters('Bell\u0007', { allowLineBreaks: true })).toBe(true)
  })
})
