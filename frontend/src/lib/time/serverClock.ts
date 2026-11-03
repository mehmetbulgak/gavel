let offsetMs = 0

export function syncServerTime(serverTime: string) {
  const parsed = Date.parse(serverTime)
  if (!Number.isNaN(parsed)) {
    offsetMs = parsed - Date.now()
  }
}

export const serverNow = () => Date.now() + offsetMs
