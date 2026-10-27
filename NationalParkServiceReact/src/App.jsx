import { useCallback, useEffect, useMemo, useRef, useState } from 'react'
import Landscape from './components/Landscape.jsx'
import ParkCard from './components/ParkCard.jsx'
import BookingDrawer from './components/BookingDrawer.jsx'
import TripsDrawer from './components/TripsDrawer.jsx'
import SignUpDrawer from './components/SignUpDrawer.jsx'
import LogInDrawer from './components/LogInDrawer.jsx'
import { ACTIVITIES, REGIONS } from './data/parks.js'
import {
  UnauthorizedError,
  cancelReservation,
  getAvailableParks,
  getMe,
  getParks,
  getReservations,
  hasToken,
  logOut,
} from './api.js'
import { formatDate, isoDaysFromNow, nightsBetween } from './dates.js'
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
  })
  const [query, setQuery] = useState('')
  // The dates last searched for; parks with nothing free on those nights are hidden.
  const [dates, setDates] = useState({ checkIn: search.checkIn, checkOut: search.checkOut })
  // Free sites per park ID for the dates in `key`; when `key` doesn't match the current one, a fetch is in flight.
  const [availability, setAvailability] = useState({ key: null, sitesLeft: new Map(), error: '' })
  // Bumped to re-count free sites after a booking or cancellation.
  const [availabilityVersion, setAvailabilityVersion] = useState(0)
  const [region, setRegion] = useState('All')
  const [activity, setActivity] = useState('')
  const [sort, setSort] = useState('recommended')
  const [favorites, setFavorites] = useState(() => new Set())
  const [bookingPark, setBookingPark] = useState(null)
  const [trips, setTrips] = useState([])
  const [tripsOpen, setTripsOpen] = useState(false)
  const [user, setUser] = useState(null)
  // { mode: 'login' | 'signup', message } while the log in or sign up drawer is open.
  const [authDrawer, setAuthDrawer] = useState(null)
  // The park the user tried to reserve before logging in; its booking drawer opens once they're in.
  const [pendingPark, setPendingPark] = useState(null)
  const [parks, setParks] = useState([])
  const [parksStatus, setParksStatus] = useState('loading')
  const resultsRef = useRef(null)

  useEffect(() => {
    getParks()
      .then((data) => {
        setParks(data)
        setParksStatus('ready')
      })
      .catch((err) => {
        console.error('Failed to load parks', err)
        setParksStatus('error')
      })
    // A token saved from an earlier visit may have expired; getMe fails and the user starts logged out.
    if (hasToken()) {
      getMe()
        .then(setUser)
        .catch((err) => console.error('Failed to restore session', err))
    }
  }, [])

  const nights = nightsBetween(dates.checkIn, dates.checkOut)
  let dateError = ''
  if (dates.checkIn < isoDaysFromNow(0)) dateError = "Check-in can't be in the past."
  else if (nights < 1) dateError = 'Check-out must be after check-in.'
  else if (nights > 14) dateError = 'Stays are limited to 14 nights.'

  const availabilityKey = dateError ? null : `${dates.checkIn}|${dates.checkOut}|${availabilityVersion}`

  useEffect(() => {
    if (!availabilityKey) return
    let stale = false
    getAvailableParks(dates.checkIn, dates.checkOut)
      .then((rows) => {
        const sitesLeft = new Map(rows.map((r) => [r.parkId, r.sitesLeft]))
        if (!stale) setAvailability({ key: availabilityKey, sitesLeft, error: '' })
      })
      .catch((err) => !stale && setAvailability({ key: availabilityKey, sitesLeft: new Map(), error: err.message }))
    return () => {
      stale = true
    }
  }, [availabilityKey, dates.checkIn, dates.checkOut])

  // Only the first count for a set of dates shows the loading message; re-counts keep the current cards up.
  const availabilityLoading =
    Boolean(availabilityKey) && !availability.key?.startsWith(`${dates.checkIn}|${dates.checkOut}|`)

  useEffect(() => {
    if (!user) return
    getReservations()
      .then(setTrips)
      .catch((err) => {
        if (err instanceof UnauthorizedError) handleSessionEnded()
        else console.error('Failed to load reservations', err)
      })
  }, [user])

  function handleLoggedIn(loggedInUser) {
    setUser(loggedInUser)
    setAuthDrawer(null)
    if (pendingPark) {
      setBookingPark(pendingPark)
      setPendingPark(null)
    }
  }

  function handleLogOut() {
    logOut()
    setUser(null)
    setTrips([])
    setTripsOpen(false)
  }

  function handleSessionEnded() {
    setUser(null)
    setTrips([])
    setBookingPark(null)
    setTripsOpen(false)
    setAuthDrawer({ mode: 'login', message: 'Your session has ended. Please log in again.' })
  }

  function handleReserve(park) {
    if (user) {
      setBookingPark(park)
    } else {
      setPendingPark(park)
      setAuthDrawer({ mode: 'login', message: 'Log in or create an account to reserve a campsite.' })
    }
  }

  function openTrips() {
    if (user) setTripsOpen(true)
    else setAuthDrawer({ mode: 'login', message: 'Log in to see your trips.' })
  }

  async function handleCancelTrip(code) {
    try {
      await cancelReservation(code)
      setTrips((t) => t.filter((x) => x.code !== code))
      setAvailabilityVersion((v) => v + 1)
    } catch (err) {
      if (err instanceof UnauthorizedError) return handleSessionEnded()
      console.error('Failed to cancel reservation', err)
      alert(`Couldn't cancel ${code}: ${err.message}`)
    }
  }

  const parkNames = useMemo(() => parks.map((p) => p.name), [parks])

  const results = useMemo(() => {
    const q = query.trim().toLowerCase()
    return parks
      .filter(
        (p) =>
          (!q || p.name.toLowerCase().includes(q) || p.state.toLowerCase().includes(q)) &&
          (region === 'All' || p.region === region) &&
          (!activity || p.activities.includes(activity)) &&
          availability.sitesLeft.has(p.id),
      )
      .map((p) => ({ ...p, sitesLeft: availability.sitesLeft.get(p.id) }))
      .sort(SORTS[sort])
  }, [parks, query, region, activity, sort, availability])

  function updateSearch(field, value) {
    setSearch((s) => ({ ...s, [field]: value }))
  }

  function handleSearch(e) {
    e.preventDefault()
    setQuery(search.destination)
    setDates({ checkIn: search.checkIn, checkOut: search.checkOut })
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
  const closeAuth = useCallback(() => {
    setAuthDrawer(null)
    setPendingPark(null)
  }, [])

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
          {user ? (
            <>
              <span className="greeting">Hi, {user.firstName}</span>
              <button type="button" className="nav-link" onClick={handleLogOut}>
                Log out
              </button>
            </>
          ) : (
            <>
              <button
                type="button"
                className="nav-link"
                onClick={() => setAuthDrawer({ mode: 'login' })}
              >
                Log in
              </button>
              <button
                type="button"
                className="trips-btn"
                onClick={() => setAuthDrawer({ mode: 'signup' })}
              >
                Sign up
              </button>
            </>
          )}
          <button type="button" className="trips-btn" onClick={openTrips}>
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
            <select
              value={search.destination}
              onChange={(e) => updateSearch('destination', e.target.value)}
            >
              <option value="">Any park</option>
              {parkNames.map((name) => (
                <option key={name}>{name}</option>
              ))}
            </select>
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
            <h2>National Parks</h2>
            <p>
              {results.length} park{results.length === 1 ? '' : 's'}
              {query && <> matching “{query}”</>} with sites free {formatDate(dates.checkIn)} –{' '}
              {formatDate(dates.checkOut)}
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

        {parksStatus === 'loading' || availabilityLoading ? (
          <div className="no-results">
            <p>Loading parks…</p>
          </div>
        ) : parksStatus === 'error' ? (
          <div className="no-results">
            <p>Couldn't load parks. Make sure the API is running, then refresh.</p>
          </div>
        ) : dateError ? (
          <div className="no-results">
            <p>{dateError} Pick new dates and search again.</p>
          </div>
        ) : availability.error ? (
          <div className="no-results">
            <p>Couldn't check availability: {availability.error}</p>
          </div>
        ) : results.length > 0 ? (
          <div className="grid">
            {results.map((park) => (
              <ParkCard
                key={park.id}
                park={park}
                isFavorite={favorites.has(park.id)}
                onToggleFavorite={toggleFavorite}
                onReserve={handleReserve}
              />
            ))}
          </div>
        ) : (
          <div className="no-results">
            <p>No parks have sites free for those dates and filters.</p>
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
          onConfirm={(trip) => {
            setTrips((t) => [trip, ...t])
            setAvailabilityVersion((v) => v + 1)
          }}
          onSessionEnded={handleSessionEnded}
        />
      )}

      {authDrawer?.mode === 'login' && (
        <LogInDrawer
          message={authDrawer.message}
          onClose={closeAuth}
          onLogIn={handleLoggedIn}
          onSwitchToSignUp={() => setAuthDrawer({ mode: 'signup' })}
        />
      )}

      {authDrawer?.mode === 'signup' && (
        <SignUpDrawer
          onClose={closeAuth}
          onSignUp={handleLoggedIn}
          onSwitchToLogIn={() => setAuthDrawer({ mode: 'login', message: authDrawer.message })}
        />
      )}

      {tripsOpen && (
        <TripsDrawer
          trips={trips}
          onCancel={handleCancelTrip}
          onClose={closeTrips}
        />
      )}
    </>
  )
}

export default App
