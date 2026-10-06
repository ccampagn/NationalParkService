import Landscape from './Landscape.jsx'

export default function ParkCard({ park, isFavorite, onToggleFavorite, onReserve }) {
  const lowAvailability = park.sitesLeft <= 5

  return (
    <article className="park-card">
      <div className="park-art">
        <Landscape scene={park.scene} palette={park.palette} />
        {park.badge && <span className="badge">{park.badge}</span>}
        <button
          type="button"
          className={`fav ${isFavorite ? 'on' : ''}`}
          aria-label={isFavorite ? 'Remove from saved' : 'Save park'}
          aria-pressed={isFavorite}
          onClick={() => onToggleFavorite(park.id)}
        >
          <svg viewBox="0 0 24 24" aria-hidden="true">
            <path d="M12 21s-7.5-4.6-9.7-9.3C.8 8.3 3 4.5 6.7 4.5c2.1 0 3.6 1.1 5.3 3 1.7-1.9 3.2-3 5.3-3 3.7 0 5.9 3.8 4.4 7.2C19.5 16.4 12 21 12 21z" />
          </svg>
        </button>
      </div>

      <div className="park-body">
        <div className="park-head">
          <h3>{park.name}</h3>
          <span className="rating" title={`${park.reviews.toLocaleString()} reviews`}>
            ★ {park.rating}
          </span>
        </div>
        <p className="park-state">{park.state}</p>
        <p className="tagline">{park.tagline}</p>
        <ul className="tags">
          {park.activities.map((a) => (
            <li key={a}>{a}</li>
          ))}
        </ul>

        <div className="park-foot">
          <div className="price">
            <span className="from">from</span>
            <strong>${park.baseRate}</strong>
            <span className="per">/ night</span>
            <span className={`left ${lowAvailability ? 'low' : ''}`}>
              {park.sitesLeft} sites left
            </span>
          </div>
          <button type="button" className="btn btn-primary" onClick={() => onReserve(park)}>
            Reserve
          </button>
        </div>
      </div>
    </article>
  )
}
