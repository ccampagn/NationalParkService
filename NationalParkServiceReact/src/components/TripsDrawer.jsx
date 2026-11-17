import { useEffect, useState } from 'react'
import Drawer from './Drawer.jsx'
import SitePicker from './SitePicker.jsx'
import { getSites } from '../api.js'
import { formatDate, isoDaysFromNow, nightsBetween } from '../dates.js'
import { CANCELLATION_FEE, CHANGE_FEE, cancellationRefund, changeFeeFor, isLateCancellation } from '../policy.js'

export default function TripsDrawer({ trips, onCancel, onChange, onClose }) {
  // A trip stays current through its check-out day. Dates are 'yyyy-MM-dd', so they compare as strings.
  const today = isoDaysFromNow(0)
  const current = trips.filter((t) => t.checkOut >= today).sort((a, b) => a.checkIn.localeCompare(b.checkIn))
  const past = trips.filter((t) => t.checkOut < today).sort((a, b) => b.checkIn.localeCompare(a.checkIn))
  // The code of the trip whose receipt is showing. Looked up in `trips` so it reflects changes, and goes away if cancelled.
  const [receiptCode, setReceiptCode] = useState(null)
  const receiptTrip = trips.find((t) => t.code === receiptCode)
  // Shown after a cancellation, e.g. how much is refunded.
  const [notice, setNotice] = useState('')

  async function cancel(code) {
    const cancellation = await onCancel(code)
    if (cancellation) setNotice(`Cancelled ${code}. $${cancellation.refund} will be refunded.`)
  }

  if (receiptTrip) {
    return (
      <Drawer title="Receipt" onClose={onClose}>
        <Receipt trip={receiptTrip} onBack={() => setReceiptCode(null)} />
      </Drawer>
    )
  }

  return (
    <Drawer title="My trips" onClose={onClose}>
      <div className="drawer-body">
        <h2 className="trips-title">My trips</h2>
        {notice && <p className="trips-notice">{notice}</p>}
        {trips.length === 0 ? (
          <p className="empty">No reservations yet. Pick a park and start planning.</p>
        ) : (
          <>
            <h3 className="trips-section">Current &amp; upcoming</h3>
            {current.length === 0 ? (
              <p className="empty">No upcoming trips. Pick a park and start planning.</p>
            ) : (
              <ul className="trip-list">
                {current.map((t) => (
                  <Trip
                    key={t.code}
                    trip={t}
                    today={today}
                    onOpen={setReceiptCode}
                    onCancel={cancel}
                    onChange={onChange}
                  />
                ))}
              </ul>
            )}
            {past.length > 0 && (
              <>
                <h3 className="trips-section">Past trips</h3>
                <ul className="trip-list past">
                  {past.map((t) => (
                    <Trip key={t.code} trip={t} today={today} onOpen={setReceiptCode} />
                  ))}
                </ul>
              </>
            )}
          </>
        )}
      </div>
    </Drawer>
  )
}

// Without onCancel and onChange, the trip is shown read-only.
function Trip({ trip: t, today, onOpen, onCancel, onChange }) {
  const [editing, setEditing] = useState(false)
  // { saving, error } while asking whether to cancel.
  const [cancelling, setCancelling] = useState(null)
  // Trips can only change before the check-in day, and only when booked on a specific site.
  const canChange = onChange && t.checkIn > today && t.siteId
  // Trips can be cancelled up to and including the check-in day.
  const canCancel = onCancel && t.checkIn >= today
  const late = isLateCancellation(t, today)
  const refund = cancellationRefund(t, today)

  async function confirmCancel() {
    setCancelling({ saving: true, error: '' })
    try {
      // Removes the trip from the list, so nothing to reset on success.
      await onCancel(t.code)
    } catch (err) {
      setCancelling({ saving: false, error: `Couldn't cancel: ${err.message}` })
    }
  }

  return (
    <li className="trip">
      <div className="trip-main">
        <button type="button" className="trip-info" onClick={() => onOpen(t.code)} aria-label={`View receipt for ${t.code}`}>
          <strong>{t.parkName}</strong>
          <span>
            {t.campground}
            {t.siteNumber && <> · Site {t.siteNumber}</>} · {formatDate(t.checkIn)} – {formatDate(t.checkOut)} · {t.guests}{' '}
            guest{t.guests === 1 ? '' : 's'}
          </span>
          <code>{t.code}</code>
          <em>View receipt</em>
        </button>
        <div className="trip-side">
          <span>${t.total}</span>
          {canChange && !editing && !cancelling && (
            <button type="button" className="link neutral" onClick={() => setEditing(true)}>
              Change trip
            </button>
          )}
          {canCancel && !editing && !cancelling && (
            <button type="button" className="link" onClick={() => setCancelling({ saving: false, error: '' })}>
              Cancel
            </button>
          )}
        </div>
      </div>

      {editing && <TripEditor trip={t} today={today} onChange={onChange} onDone={() => setEditing(false)} />}

      {cancelling && (
        <div className="trip-edit">
          <p className="trip-cancel-title">Cancel this trip?</p>
          <dl className="summary">
            <div>
              <dt>Paid</dt>
              <dd>${t.total}</dd>
            </div>
            <div>
              <dt>Reservation fee (non-refundable)</dt>
              <dd>−${t.reservationFee}</dd>
            </div>
            {t.changeFees > 0 && (
              <div>
                <dt>Change fees (non-refundable)</dt>
                <dd>−${t.changeFees}</dd>
              </div>
            )}
            {late && t.nights === 1 ? (
              <div>
                <dt>One-night stay cancelled the day before or day of arrival</dt>
                <dd>No refund</dd>
              </div>
            ) : (
              <>
                <div>
                  <dt>Cancellation fee</dt>
                  <dd>−${CANCELLATION_FEE}</dd>
                </div>
                {late && (
                  <div>
                    <dt>First night (cancelled the day before or day of arrival)</dt>
                    <dd>−${t.nightlyRate}</dd>
                  </div>
                )}
              </>
            )}
            <div className="total">
              <dt>Refund</dt>
              <dd>${refund}</dd>
            </div>
          </dl>
          {cancelling.error && <p className="error">{cancelling.error}</p>}
          <div className="trip-edit-actions">
            <button type="button" className="btn btn-ghost" onClick={() => setCancelling(null)} disabled={cancelling.saving}>
              Keep trip
            </button>
            <button type="button" className="btn btn-danger" onClick={confirmCancel} disabled={cancelling.saving}>
              {cancelling.saving ? 'Cancelling…' : 'Cancel trip'}
            </button>
          </div>
        </div>
      )}
    </li>
  )
}

function TripEditor({ trip: t, today, onChange, onDone }) {
  const [checkIn, setCheckIn] = useState(t.checkIn)
  const [checkOut, setCheckOut] = useState(t.checkOut)
  const [siteId, setSiteId] = useState(t.siteId)
  // Sites for the dates in `key`; when `key` doesn't match the current one, a fetch is in flight.
  const [sitesResult, setSitesResult] = useState({ key: null, sites: [], error: '' })
  const [saving, setSaving] = useState(false)
  const [saveError, setSaveError] = useState('')

  const nights = nightsBetween(checkIn, checkOut)
  let dateError = ''
  if (checkIn < today) dateError = "Check-in can't be in the past."
  else if (nights < 1) dateError = 'Check-out must be after check-in.'
  else if (nights > 14) dateError = 'Stays are limited to 14 nights.'

  const sitesKey = dateError ? null : `${checkIn}|${checkOut}`

  useEffect(() => {
    if (!sitesKey) return
    let stale = false
    // `except` so this trip's own booking doesn't make its current site look taken.
    getSites(t.campgroundId, checkIn, checkOut, t.code)
      .then((sites) => !stale && setSitesResult({ key: sitesKey, sites, error: '' }))
      .catch((err) => !stale && setSitesResult({ key: sitesKey, sites: [], error: err.message }))
    return () => {
      stale = true
    }
  }, [sitesKey, t.campgroundId, t.code, checkIn, checkOut])

  const sitesLoading = Boolean(sitesKey) && sitesResult.key !== sitesKey
  const sites = sitesKey && !sitesLoading ? sitesResult.sites : []
  const fits = (s) => s.available && s.maxGuests >= t.guests
  // A choice that's booked for the new dates no longer counts.
  const site = sites.find((s) => s.siteId === siteId && fits(s))
  // Staying on the same site keeps the rate it was booked at; the API does the same.
  const rate = site && site.siteId !== t.siteId ? site.nightlyRate : t.nightlyRate
  const changeFee = changeFeeFor(t, checkIn, checkOut)
  const total = nights * rate + t.reservationFee + t.changeFees + changeFee
  const unchanged = checkIn === t.checkIn && checkOut === t.checkOut && siteId === t.siteId

  async function save(e) {
    e.preventDefault()
    setSaving(true)
    setSaveError('')
    try {
      // The API checks the season and other bookings and works out the new total.
      await onChange(t.code, { siteId: site.siteId, checkIn, checkOut })
      onDone()
    } catch (err) {
      setSaveError(`Couldn't change your trip: ${err.message}`)
      setSaving(false)
    }
  }

  return (
    <form className="trip-edit" onSubmit={save}>
      <div className="field-row">
        <label className="field">
          <span>Check in</span>
          <input type="date" value={checkIn} min={today} onChange={(e) => setCheckIn(e.target.value)} />
        </label>
        <label className="field">
          <span>Check out</span>
          <input type="date" value={checkOut} min={checkIn} onChange={(e) => setCheckOut(e.target.value)} />
        </label>
      </div>

      {dateError ? null : sitesLoading ? (
        <p className="site-note">Checking availability…</p>
      ) : sitesResult.error ? (
        <p className="error">Couldn't load sites: {sitesResult.error}</p>
      ) : (
        <SitePicker
          sites={sites}
          selectedId={site?.siteId}
          fits={fits}
          onSelect={setSiteId}
          name={`site-${t.code}`}
          describe={(s) => [
            s.siteId === t.siteId ? 'Current' : s.siteType,
            !s.available
              ? 'Booked'
              : !fits(s)
                ? `Fits ${s.maxGuests}`
                : `$${s.siteId === t.siteId ? t.nightlyRate : s.nightlyRate} · ${s.maxGuests} max`,
          ]}
        />
      )}

      {!dateError && site && !unchanged && (
        <p className="site-note">
          New total ${total} (was ${t.total})
          {changeFee > 0 && <> · includes a ${changeFee} change fee for entirely new dates</>}
        </p>
      )}
      {(dateError || saveError) && <p className="error">{dateError || saveError}</p>}

      <p className="fine">
        Extending or shortening your stay, or moving sites, is free. Moving to dates that share no nights with your
        current ones costs ${CHANGE_FEE}.
      </p>

      <div className="trip-edit-actions">
        <button type="button" className="btn btn-ghost" onClick={onDone} disabled={saving}>
          Keep trip
        </button>
        <button type="submit" className="btn btn-primary" disabled={Boolean(dateError) || !site || unchanged || saving}>
          {saving ? 'Saving…' : site ? 'Save changes' : 'Choose a site'}
        </button>
      </div>
    </form>
  )
}

function Receipt({ trip: t, onBack }) {
  const bookedOn = new Date(t.bookedAt).toLocaleDateString('en-US', { month: 'long', day: 'numeric', year: 'numeric' })
  const fullDate = (iso) =>
    new Date(iso + 'T00:00').toLocaleDateString('en-US', { weekday: 'short', month: 'short', day: 'numeric', year: 'numeric' })

  return (
    <div className="drawer-body receipt">
      <button type="button" className="link neutral back" onClick={onBack}>
        ← Back to trips
      </button>
      <h2 className="trips-title">{t.parkName}</h2>
      <p className="receipt-code">
        Confirmation <strong>{t.code}</strong>
        <span>Booked {bookedOn}</span>
      </p>

      <dl className="summary">
        <div>
          <dt>Campground</dt>
          <dd>{t.campground}</dd>
        </div>
        {t.siteNumber && (
          <div>
            <dt>Site</dt>
            <dd>
              {t.siteNumber} · {t.siteType}
            </dd>
          </div>
        )}
        <div>
          <dt>Check in</dt>
          <dd>{fullDate(t.checkIn)}</dd>
        </div>
        <div>
          <dt>Check out</dt>
          <dd>{fullDate(t.checkOut)}</dd>
        </div>
        <div>
          <dt>Guests</dt>
          <dd>{t.guests}</dd>
        </div>
      </dl>

      <dl className="summary">
        <div>
          <dt>
            ${t.nightlyRate} × {t.nights} night{t.nights === 1 ? '' : 's'}
          </dt>
          <dd>${t.nightlyRate * t.nights}</dd>
        </div>
        <div>
          <dt>Reservation fee</dt>
          <dd>${t.reservationFee}</dd>
        </div>
        {t.changeFees > 0 && (
          <div>
            <dt>Change fees</dt>
            <dd>${t.changeFees}</dd>
          </div>
        )}
        <div className="total">
          <dt>Total paid</dt>
          <dd>${t.total}</dd>
        </div>
      </dl>

      <p className="fine">Show your confirmation code at the gate.</p>
    </div>
  )
}
