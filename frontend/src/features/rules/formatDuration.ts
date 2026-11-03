const units = [
  { minutes: 1440, singular: 'day', plural: 'days' },
  { minutes: 60, singular: 'hour', plural: 'hours' },
  { minutes: 1, singular: 'minute', plural: 'minutes' },
] as const

export function formatDuration(totalMinutes: number) {
  const unit = units.find((u) => totalMinutes >= u.minutes && totalMinutes % u.minutes === 0) ?? units[2]
  const count = totalMinutes / unit.minutes
  return `${count} ${count === 1 ? unit.singular : unit.plural}`
}

export function formatShortDuration(totalMinutes: number) {
  if (totalMinutes % 1440 === 0) return `${totalMinutes / 1440}d`
  if (totalMinutes % 60 === 0) return `${totalMinutes / 60}h`
  return `${totalMinutes}m`
}
