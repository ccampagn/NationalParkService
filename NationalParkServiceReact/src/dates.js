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

export function addDays(iso, days) {
  const d = new Date(iso + 'T00:00')
  d.setDate(d.getDate() + days)
  const pad = (n) => String(n).padStart(2, '0')
  return `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}`
}

// Same rules as Campground.IsOpenOn in the API. seasonOpens and seasonCloses are the first and last nights as
// 'MM-dd' and may run across New Year; without them the campground is open year-round.
export function isOpenOn(campground, night) {
  const { seasonOpens: open, seasonCloses: close } = campground
  if (!open || !close) return true
  const day = night.slice(5)
  return open <= close ? day >= open && day <= close : day >= open || day <= close
}

// The first night from checkIn up to (not including) checkOut that the campground is closed, or null.
export function firstClosedNight(campground, checkIn, checkOut) {
  for (let night = checkIn; night < checkOut; night = addDays(night, 1)) {
    if (!isOpenOn(campground, night)) return night
  }
  return null
}

// e.g. 'Open May 22 – Sep 20', or 'Open year-round'.
export function formatSeason(campground) {
  const { seasonOpens: open, seasonCloses: close } = campground
  if (!open || !close) return 'Open year-round'
  return `Open ${formatDate('2000-' + open)} – ${formatDate('2000-' + close)}`
}
