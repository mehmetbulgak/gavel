const currency = new Intl.NumberFormat('tr-TR', { style: 'currency', currency: 'TRY', maximumFractionDigits: 2 })
const wholeCurrency = new Intl.NumberFormat('tr-TR', { style: 'currency', currency: 'TRY', maximumFractionDigits: 0 })
const number = new Intl.NumberFormat('en-US')
const dateTime = new Intl.DateTimeFormat('en-GB', { day: 'numeric', month: 'short', hour: '2-digit', minute: '2-digit' })
const time = new Intl.DateTimeFormat('en-GB', { hour: '2-digit', minute: '2-digit', second: '2-digit' })
const shortTime = new Intl.DateTimeFormat('en-GB', { hour: '2-digit', minute: '2-digit' })

export const formatMoney = (value: number) => (Number.isInteger(value) ? wholeCurrency.format(value) : currency.format(value))

export const formatNumber = (value: number) => number.format(value)

export const formatDateTime = (value: string) => dateTime.format(new Date(value))

export const formatClock = (value: string) => time.format(new Date(value))

export const formatShortTime = (value: number) => shortTime.format(new Date(value))

export const pluralize = (count: number, singular: string, plural = `${singular}s`) =>
  `${formatNumber(count)} ${count === 1 ? singular : plural}`

export function formatRemaining(milliseconds: number) {
  if (milliseconds <= 0) return 'Ended'
  const totalSeconds = Math.floor(milliseconds / 1000)
  const days = Math.floor(totalSeconds / 86_400)
  const hours = Math.floor((totalSeconds % 86_400) / 3600)
  const minutes = Math.floor((totalSeconds % 3600) / 60)
  const seconds = totalSeconds % 60

  if (days > 0) return `${days}d ${hours}h`
  if (hours > 0) return `${hours}h ${minutes}m`
  if (minutes > 0) return `${minutes}m ${seconds.toString().padStart(2, '0')}s`
  return `${seconds}s`
}

export const paddleNumber = (alias: string | null | undefined) => {
  const match = alias?.match(/(\d+)$/)
  return match ? Number(match[1]) : null
}
