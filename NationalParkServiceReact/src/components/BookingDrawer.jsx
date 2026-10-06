import { useState } from 'react'
import Drawer from './Drawer.jsx'
import Landscape from './Landscape.jsx'
import { RESERVATION_FEE, siteOptionsFor } from '../data/parks.js'
import { formatDate, nightsBetween } from '../dates.js'

function confirmationCode() {
  return 'NPS-' + Math.random().toString(36).slice(2, 8).toUpperCase()
}

export default function BookingDrawer({ park, initial, onClose, onConfirm }) {
  const sites = siteOptionsFor(park)
  const [siteId, setSiteId] = useState(sites[0].id)
  const [checkIn, setCheckIn] = useState(initial.checkIn)
  const [checkOut, setCheckOut] = useState(initial.checkOut)
  const [guests, setGuests] = useState(initial.guests)
  const [confirmed, setConfirmed] = useState(null)

  const site = sites.find((s) => s.id === siteId)
  const nights = nightsBetween(checkIn, checkOut)
  const subtotal = nights * site.rate
  const total = subtotal + RESERVATION_FEE

  let error = ''
  if (nights < 1) error = 'Check-out must be after check-in.'
  else if (nights > 14) error = 'Stays are limited to 14 nights.'
  else if (guests > site.capacity) error = `${site.label} fits up to ${site.capacity} guests.`

  function handleConfirm() {
    const trip = {
      code: confirmationCode(),
      parkId: park.id,
      parkName: park.name,
      site: site.label,
      checkIn,
      checkOut,
      nights,
      guests,
      total,
    }
    onConfirm(trip)
    setConfirmed(trip)
  }

  return (
    <Drawer title={`Reserve at ${park.name}`} onClose={onClose}>
      <div className="drawer-art">
        <Landscape scene={park.scene} palette={park.palette} />
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
              <dt>Site</dt>
              <dd>{confirmed.site}</dd>
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
          <h3 className="step">1 · Choose a site</h3>
          <div className="site-options" role="radiogroup" aria-label="Site type">
            {sites.map((s) => (
              <label key={s.id} className={`site ${s.id === siteId ? 'selected' : ''}`}>
                <input
                  type="radio"
                  name="site"
                  value={s.id}
                  checked={s.id === siteId}
                  onChange={() => setSiteId(s.id)}
                />
                <span className="site-name">{s.label}</span>
                <span className="site-rate">${s.rate}</span>
                <span className="site-detail">
                  {s.detail} · up to {s.capacity}
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
                onClick={() => setGuests((g) => Math.min(12, g + 1))}
              >
                +
              </button>
            </div>
          </div>

          <dl className="summary">
            <div>
              <dt>
                ${site.rate} × {Math.max(nights, 0)} night{nights === 1 ? '' : 's'}
              </dt>
              <dd>${Math.max(subtotal, 0)}</dd>
            </div>
            <div>
              <dt>Reservation fee</dt>
              <dd>${RESERVATION_FEE}</dd>
            </div>
            <div className="total">
              <dt>Total</dt>
              <dd>${error ? '—' : total}</dd>
            </div>
          </dl>

          {error && <p className="error">{error}</p>}

          <button
            type="button"
            className="btn btn-primary wide"
            disabled={Boolean(error)}
            onClick={handleConfirm}
          >
            Confirm reservation
          </button>
          <p className="fine">Free cancellation up to 48 hours before arrival.</p>
        </div>
      )}
    </Drawer>
  )
}
