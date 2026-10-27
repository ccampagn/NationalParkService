import { useEffect, useState } from 'react'
import Drawer from './Drawer.jsx'
import ParkPhoto from './ParkPhoto.jsx'
import { RESERVATION_FEE } from '../data/parks.js'
import { UnauthorizedError, createReservation, getSites } from '../api.js'
import { firstClosedNight, formatDate, formatSeason, isoDaysFromNow, nightsBetween } from '../dates.js'

export default function BookingDrawer({ park, initial, onClose, onConfirm, onSessionEnded }) {
  const campgrounds = park.campgrounds
  // Start on a campground that's open for the suggested dates, if there is one.
  const [campgroundId, setCampgroundId] = useState(
    () => (campgrounds.find((c) => !firstClosedNight(c, initial.checkIn, initial.checkOut)) ?? campgrounds[0])?.id,
  )
  const [checkIn, setCheckIn] = useState(initial.checkIn)
  const [checkOut, setCheckOut] = useState(initial.checkOut)
  const [guests, setGuests] = useState(2)
  const [siteId, setSiteId] = useState(null)
  // Bumped to re-fetch availability, e.g. after someone else books the chosen site first.
  const [sitesVersion, setSitesVersion] = useState(0)
  // Sites for the campground and dates in `key`; when `key` doesn't match the current one, a fetch is in flight.
  const [sitesResult, setSitesResult] = useState({ key: null, sites: [], error: '' })
  const [confirmed, setConfirmed] = useState(null)
  const [saving, setSaving] = useState(false)
  const [saveError, setSaveError] = useState('')

  const nights = nightsBetween(checkIn, checkOut)
  let dateError = ''
  if (checkIn < isoDaysFromNow(0)) dateError = "Check-in can't be in the past."
  else if (nights < 1) dateError = 'Check-out must be after check-in.'
  else if (nights > 14) dateError = 'Stays are limited to 14 nights.'

  const campground = campgrounds.find((c) => c.id === campgroundId)
  const closedNight = campground && !dateError ? firstClosedNight(campground, checkIn, checkOut) : null
  if (closedNight) {
    dateError = `${campground.name} is closed for the season on ${formatDate(closedNight)}. ${formatSeason(campground)}.`
  }

  const sitesKey = campgroundId && !dateError ? `${campgroundId}|${checkIn}|${checkOut}|${sitesVersion}` : null

  useEffect(() => {
    if (!sitesKey) return
    let stale = false
    getSites(campgroundId, checkIn, checkOut)
      .then((sites) => !stale && setSitesResult({ key: sitesKey, sites, error: '' }))
      .catch((err) => !stale && setSitesResult({ key: sitesKey, sites: [], error: err.message }))
    return () => {
      stale = true
    }
  }, [sitesKey, campgroundId, checkIn, checkOut])

  if (campgrounds.length === 0) {
    return (
      <Drawer title={`Reserve at ${park.name}`} onClose={onClose}>
        <div className="drawer-art">
          <ParkPhoto park={park} />
          <div className="drawer-art-text">
            <span>{park.state}</span>
            <h2>{park.name}</h2>
          </div>
        </div>
        <div className="drawer-body">
          <p>{park.name} has no campgrounds to reserve.</p>
          <button type="button" className="btn btn-primary wide" onClick={onClose}>
            Close
          </button>
        </div>
      </Drawer>
    )
  }

  const sitesLoading = Boolean(sitesKey) && sitesResult.key !== sitesKey
  const sites = sitesKey && !sitesLoading ? sitesResult.sites : []
  const fits = (s) => s.available && s.maxGuests >= guests
  // A choice that's been booked or outgrown since it was picked no longer counts.
  const site = sites.find((s) => s.siteId === siteId && fits(s))
  const rate = site?.nightlyRate ?? campground.rate
  const subtotal = nights * rate
  const total = subtotal + RESERVATION_FEE
  const maxGuests = Math.max(12, ...sites.map((s) => s.maxGuests))

  function chooseCampground(id) {
    setCampgroundId(id)
    setSiteId(null)
  }

  async function handleConfirm() {
    setSaving(true)
    setSaveError('')
    try {
      // The API checks the request again, works out the price and assigns the confirmation code.
      const trip = await createReservation({ siteId: site.siteId, checkIn, checkOut, guests })
      onConfirm(trip)
      setConfirmed(trip)
    } catch (err) {
      if (err instanceof UnauthorizedError) return onSessionEnded()
      setSaveError(`Couldn't save your reservation: ${err.message}`)
      // Someone may have just taken this site; show fresh availability.
      setSitesVersion((v) => v + 1)
    } finally {
      setSaving(false)
    }
  }

  return (
    <Drawer title={`Reserve at ${park.name}`} onClose={onClose}>
      <div className="drawer-art">
        <ParkPhoto park={park} />
        <div className="drawer-art-text">
          <span>{park.state}</span>
          <h2>{park.name}</h2>
        </div>
      </div>

      {confirmed ? (
        <div className="drawer-body confirmed">
          <div className="check">✓</div>
          <h3>You're going to {park.name}!</h3>
          <p>
            Confirmation <strong>{confirmed.code}</strong>
          </p>
          <dl className="summary">
            <div>
              <dt>Campground</dt>
              <dd>{confirmed.campground}</dd>
            </div>
            <div>
              <dt>Site</dt>
              <dd>
                {confirmed.siteNumber} · {confirmed.siteType}
              </dd>
            </div>
            <div>
              <dt>Dates</dt>
              <dd>
                {formatDate(confirmed.checkIn)} – {formatDate(confirmed.checkOut)}
              </dd>
            </div>
            <div>
              <dt>Guests</dt>
              <dd>{confirmed.guests}</dd>
            </div>
            <div className="total">
              <dt>Paid</dt>
              <dd>${confirmed.total}</dd>
            </div>
          </dl>
          <button type="button" className="btn btn-primary wide" onClick={onClose}>
            Done
          </button>
        </div>
      ) : (
        <div className="drawer-body">
          <h3 className="step">1 · Choose a campground</h3>
          <div className="site-options" role="radiogroup" aria-label="Campground">
            {campgrounds.map((c) => (
              <label key={c.id} className={`site ${c.id === campgroundId ? 'selected' : ''}`}>
                <input
                  type="radio"
                  name="campground"
                  value={c.id}
                  checked={c.id === campgroundId}
                  onChange={() => chooseCampground(c.id)}
                />
                <span className="site-name">{c.name}</span>
                <span className="site-rate">from ${c.rate}</span>
                <span className="site-detail">
                  Tent, RV and group sites · {formatSeason(c)}
                  {nights >= 1 && nights <= 14 && firstClosedNight(c, checkIn, checkOut) ? ' · Closed for your dates' : ''}
                </span>
              </label>
            ))}
          </div>

          <h3 className="step">2 · Dates & guests</h3>
          <div className="field-row">
            <label className="field">
              <span>Check in</span>
              <input type="date" value={checkIn} onChange={(e) => setCheckIn(e.target.value)} />
            </label>
            <label className="field">
              <span>Check out</span>
              <input type="date" value={checkOut} onChange={(e) => setCheckOut(e.target.value)} />
            </label>
          </div>
          <div className="stepper">
            <span>Guests</span>
            <div>
              <button
                type="button"
                aria-label="Fewer guests"
                onClick={() => setGuests((g) => Math.max(1, g - 1))}
              >
                −
              </button>
              <output>{guests}</output>
              <button
                type="button"
                aria-label="More guests"
                onClick={() => setGuests((g) => Math.min(maxGuests, g + 1))}
              >
                +
              </button>
            </div>
          </div>

          <h3 className="step">3 · Choose a site</h3>
          {dateError ? (
            <p className="site-note">Pick valid dates to see which sites are free.</p>
          ) : sitesLoading ? (
            <p className="site-note">Checking availability…</p>
          ) : sitesResult.error ? (
            <p className="error">Couldn't load sites: {sitesResult.error}</p>
          ) : sites.length === 0 ? (
            <p className="site-note">{campground.name} has no bookable sites yet.</p>
          ) : (
            <div className="site-grid" role="radiogroup" aria-label="Site">
              {sites.map((s) => (
                <label
                  key={s.siteId}
                  className={`site-tile ${s.siteId === site?.siteId ? 'selected' : ''}`}
                  aria-disabled={!fits(s)}
                >
                  <input
                    type="radio"
                    name="site"
                    value={s.siteId}
                    checked={s.siteId === site?.siteId}
                    disabled={!fits(s)}
                    onChange={() => setSiteId(s.siteId)}
                  />
                  <strong>{s.siteNumber}</strong>
                  <span>{s.siteType}</span>
                  <span>
                    {!s.available ? 'Booked' : !fits(s) ? `Fits ${s.maxGuests}` : `$${s.nightlyRate} · ${s.maxGuests} max`}
                  </span>
                </label>
              ))}
            </div>
          )}

          <dl className="summary">
            <div>
              <dt>
                {site ? `Site ${site.siteNumber} · ` : ''}${rate} × {Math.max(nights, 0)} night
                {nights === 1 ? '' : 's'}
              </dt>
              <dd>${Math.max(subtotal, 0)}</dd>
            </div>
            <div>
              <dt>Reservation fee</dt>
              <dd>${RESERVATION_FEE}</dd>
            </div>
            <div className="total">
              <dt>Total</dt>
              <dd>${dateError || !site ? '—' : total}</dd>
            </div>
          </dl>

          {(dateError || saveError) && <p className="error">{dateError || saveError}</p>}

          <button
            type="button"
            className="btn btn-primary wide"
            disabled={Boolean(dateError) || !site || saving}
            onClick={handleConfirm}
          >
            {saving ? 'Saving…' : site ? `Reserve site ${site.siteNumber}` : 'Choose a site'}
          </button>
          <p className="fine">Free cancellation up to 48 hours before arrival.</p>
        </div>
      )}
    </Drawer>
  )
}
