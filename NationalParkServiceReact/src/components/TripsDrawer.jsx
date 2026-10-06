import Drawer from './Drawer.jsx'
import { formatDate } from '../dates.js'

export default function TripsDrawer({ trips, onCancel, onClose }) {
  return (
    <Drawer title="My trips" onClose={onClose}>
      <div className="drawer-body">
        <h2 className="trips-title">My trips</h2>
        {trips.length === 0 ? (
          <p className="empty">No reservations yet. Pick a park and start planning.</p>
        ) : (
          <ul className="trip-list">
            {trips.map((t) => (
              <li key={t.code} className="trip">
                <div>
                  <strong>{t.parkName}</strong>
                  <span>
                    {t.site} · {formatDate(t.checkIn)} – {formatDate(t.checkOut)} · {t.guests}{' '}
                    guest{t.guests === 1 ? '' : 's'}
                  </span>
                  <code>{t.code}</code>
                </div>
                <div className="trip-side">
                  <span>${t.total}</span>
                  <button type="button" className="link" onClick={() => onCancel(t.code)}>
                    Cancel
                  </button>
                </div>
              </li>
            ))}
          </ul>
        )}
      </div>
    </Drawer>
  )
}
