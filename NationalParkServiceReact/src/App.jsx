import { useCallback, useMemo, useRef, useState } from 'react'
import Landscape from './components/Landscape.jsx'
import ParkCard from './components/ParkCard.jsx'
import BookingDrawer from './components/BookingDrawer.jsx'
import TripsDrawer from './components/TripsDrawer.jsx'
import { ACTIVITIES, REGIONS, parks } from './data/parks.js'
import { isoDaysFromNow } from './dates.js'
import './App.css'

const HERO_PALETTE = {
  sky: ['#1b2a4a', '#f08a5d'],
  sun: '#ffd9a0',
  layers: ['#8a4f6b', '#4b3355', '#1a1a2e'],
}

const SORTS = {
  recommended: () => 0,
  price: (a, b) => a.baseRate - b.baseRate,
  rating: (a, b) => b.rating - a.rating,
}

function App() {
  const [search, setSearch] = useState({
    destination: '',
    checkIn: isoDaysFromNow(14),
    checkOut: isoDaysFromNow(17),
    guests: 2,
  })
  const [query, setQuery] = useState('')
  const [region, setRegion] = useState('All')
  const [activity, setActivity] = useState('')
  const [sort, setSort] = useState('recommended')
  const [favorites, setFavorites] = useState(() => new Set())
  const [bookingPark, setBookingPark] = useState(null)
  const [trips, setTrips] = useState([])
  const [tripsOpen, setTripsOpen] = useState(false)
  const resultsRef = useRef(null)

  const results = useMemo(() => {
    const q = query.trim().toLowerCase()
    return parks
      .filter(
        (p) =>
          (!q || p.name.toLowerCase().includes(q) || p.state.toLowerCase().includes(q)) &&
          (region === 'All' || p.region === region) &&
          (!activity || p.activities.includes(activity)),
      )
      .sort(SORTS[sort])
  }, [query, region, activity, sort])

  function updateSearch(field, value) {
    setSearch((s) => ({ ...s, [field]: value }))
  }

  function handleSearch(e) {
    e.preventDefault()
    setQuery(search.destination)
    resultsRef.current?.scrollIntoView({ behavior: 'smooth' })
  }

  function toggleFavorite(id) {
    setFavorites((prev) => {
      const next = new Set(prev)
      next.has(id) ? next.delete(id) : next.add(id)
      return next
    })
  }

  function resetFilters() {
    setQuery('')
    setRegion('All')
    setActivity('')
    updateSearch('destination', '')
  }

  const closeBooking = useCallback(() => setBookingPark(null), [])
  const closeTrips = useCallback(() => setTripsOpen(false), [])

  return (
    <>
      <header className="nav">
        <a href="#" className="logo">
          <svg viewBox="0 0 32 32" aria-hidden="true">
            <path d="M16 3 L29 27 H3 Z" fill="currentColor" />
            <path d="M16 13 L22 27 H10 Z" fill="var(--sand)" />
          </svg>
          Trailhead
        </a>
        <nav>
          <a href="#parks">Parks</a>
          <a href="#how">How it works</a>
          <button type="button" className="trips-btn" onClick={() => setTripsOpen(true)}>
            My trips
            {trips.length > 0 && <span className="count">{trips.length}</span>}
          </button>
        </nav>
      </header>

      <section className="hero">
        <Landscape scene="peaks" palette={HERO_PALETTE} className="hero-art" sun={[255, 108]} />
        <div className="stars" aria-hidden="true" />
        <div className="hero-content">
          <p className="eyebrow">Campsites · Cabins · Permits</p>
          <h1>
            Find your <em>wild.</em>
          </h1>
          <p className="lede">
            Reserve campsites, cabins and backcountry permits across America's national parks.
          </p>
        </div>

        <form className="search" onSubmit={handleSearch}>
          <label className="field grow">
            <span>Where to?</span>
            <input
              type="text"
              placeholder="Park or state"
              value={search.destination}
              onChange={(e) => updateSearch('destination', e.target.value)}
            />
          </label>
          <label className="field">
            <span>Check in</span>
            <input
              type="date"
              value={search.checkIn}
              onChange={(e) => updateSearch('checkIn', e.target.value)}
            />
          </label>
          <label className="field">
            <span>Check out</span>
            <input
              type="date"
              value={search.checkOut}
              onChange={(e) => updateSearch('checkOut', e.target.value)}
            />
          </label>
          <label className="field small">
            <span>Guests</span>
            <select
              value={search.guests}
              onChange={(e) => updateSearch('guests', Number(e.target.value))}
            >
              {[1, 2, 3, 4, 5, 6, 7, 8].map((n) => (
                <option key={n} value={n}>
                  {n}
                </option>
              ))}
            </select>
          </label>
          <button type="submit" className="btn btn-primary search-btn">
            Search
          </button>
        </form>
      </section>

      <section className="stats">
        <div>
          <strong>63</strong>
          <span>national parks</span>
        </div>
        <div>
          <strong>4,200+</strong>
          <span>bookable sites</span>
        </div>
        <div>
          <strong>85M</strong>
          <span>acres to explore</span>
        </div>
        <div>
          <strong>4.8★</strong>
          <span>average rating</span>
        </div>
      </section>

      <main id="parks" ref={resultsRef} className="parks">
        <div className="parks-header">
          <div>
            <h2>Popular parks</h2>
            <p>
              {results.length} park{results.length === 1 ? '' : 's'}
              {query && <> matching “{query}”</>}
            </p>
          </div>
          <div className="controls">
            <select
              value={activity}
              onChange={(e) => setActivity(e.target.value)}
              aria-label="Activity"
            >
              <option value="">Any activity</option>
              {ACTIVITIES.map((a) => (
                <option key={a}>{a}</option>
              ))}
            </select>
            <select value={sort} onChange={(e) => setSort(e.target.value)} aria-label="Sort by">
              <option value="recommended">Recommended</option>
              <option value="price">Lowest price</option>
              <option value="rating">Top rated</option>
            </select>
          </div>
        </div>

        <div className="chips" role="tablist" aria-label="Region">
          {REGIONS.map((r) => (
            <button
              key={r}
              type="button"
              role="tab"
              aria-selected={region === r}
              className={`chip ${region === r ? 'active' : ''}`}
              onClick={() => setRegion(r)}
            >
              {r}
            </button>
          ))}
        </div>

        {results.length > 0 ? (
          <div className="grid">
            {results.map((park) => (
              <ParkCard
                key={park.id}
                park={park}
                isFavorite={favorites.has(park.id)}
                onToggleFavorite={toggleFavorite}
                onReserve={setBookingPark}
              />
            ))}
          </div>
        ) : (
          <div className="no-results">
            <p>No parks match those filters.</p>
            <button type="button" className="btn btn-ghost" onClick={resetFilters}>
              Clear filters
            </button>
          </div>
        )}
      </main>

      <section id="how" className="how">
        <h2>Three steps to the trailhead</h2>
        <ol>
          <li>
            <span>01</span>
            <h3>Pick a park</h3>
            <p>Filter by region, activity or rating to find your spot.</p>
          </li>
          <li>
            <span>02</span>
            <h3>Choose your site</h3>
            <p>Tent, RV, cabin or a backcountry permit.</p>
          </li>
          <li>
            <span>03</span>
            <h3>Pack the car</h3>
            <p>Your confirmation code is all you need at the gate.</p>
          </li>
        </ol>
      </section>

      <footer className="footer">
        <span>© 2026 Trailhead Reservations · Demo data</span>
        <span>Leave no trace.</span>
      </footer>

      {bookingPark && (
        <BookingDrawer
          park={bookingPark}
          initial={search}
          onClose={closeBooking}
          onConfirm={(trip) => setTrips((t) => [trip, ...t])}
        />
      )}

      {tripsOpen && (
        <TripsDrawer
          trips={trips}
          onCancel={(code) => setTrips((t) => t.filter((x) => x.code !== code))}
          onClose={closeTrips}
        />
      )}
    </>
  )
}

export default App
