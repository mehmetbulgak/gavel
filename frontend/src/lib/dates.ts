const pad = (value: number) => value.toString().padStart(2, '0')

export function toLocalInputValue(time: number) {
  const date = new Date(time)
  return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}T${pad(date.getHours())}:${pad(date.getMinutes())}`
}
