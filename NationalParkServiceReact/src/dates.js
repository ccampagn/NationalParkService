export function isoDaysFromNow(days) {
  const d = new Date()
  d.setDate(d.getDate() + days)
  const pad = (n) => String(n).padStart(2, '0')
  return `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}`
}

export function nightsBetween(checkIn, checkOut) {
  if (!checkIn || !checkOut) return 0
  return Math.round((new Date(checkOut) - new Date(checkIn)) / 86_400_000)
}

export function formatDate(iso) {
  return new Date(iso + 'T00:00').toLocaleDateString('en-US', {
    month: 'short',
    day: 'numeric',
  })
}
