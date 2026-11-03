const ALLOWED_WHITESPACE = new Set(['\n', '\r', '\t'])

const isControl = (code: number) => code < 0x20 || (code >= 0x7f && code <= 0x9f)

export function hasControlCharacters(value: string, { allowLineBreaks = false } = {}) {
  for (const char of value) {
    if (allowLineBreaks && ALLOWED_WHITESPACE.has(char)) continue
    if (isControl(char.charCodeAt(0))) return true
  }
  return false
}
