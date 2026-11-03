const STEPS = [1, 2, 2.5, 5, 10]

export function niceTicks(min: number, max: number, count = 4) {
  if (max <= min) {
    const top = max === 0 ? 1 : max * 1.1
    return niceTicks(0, top, count)
  }

  const rough = (max - min) / count
  const magnitude = 10 ** Math.floor(Math.log10(rough))
  const step = (STEPS.find((s) => s * magnitude >= rough) ?? 10) * magnitude
  const start = Math.floor(min / step) * step
  const ticks: number[] = []
  for (let value = start; value <= max + step * 0.999; value += step) {
    ticks.push(Number(value.toFixed(2)))
  }

  return ticks
}
