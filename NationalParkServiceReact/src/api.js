import { fromApi } from './data/parks.js'

// Empty in development (Vite proxies /api to the local API); set VITE_API_URL in .env.production for the deployed API.
const API_URL = (import.meta.env.VITE_API_URL ?? '').replace(/\/$/, '')
const TOKEN_KEY = 'trailhead.token'

// The bearer token from log in, kept so a refresh doesn't log the user out. Storage can be unavailable (private mode).
let token = null
try {
  token = localStorage.getItem(TOKEN_KEY)
} catch {
  // Ignore; the user will have to log in again after a refresh.
}

function setToken(value) {
  token = value
  try {
    value ? localStorage.setItem(TOKEN_KEY, value) : localStorage.removeItem(TOKEN_KEY)
  } catch {
    // Ignore, as above.
  }
}

export function hasToken() {
  return Boolean(token)
}

// Thrown when the API says the token is missing or expired, so the app can log the user out.
export class UnauthorizedError extends Error {}

async function request(path, options = {}) {
  const headers = { ...options.headers }
  if (token) headers.Authorization = `Bearer ${token}`
  const res = await fetch(`${API_URL}/api${path}`, { ...options, headers })
  if (res.status === 401 && token) {
    setToken(null)
    throw new UnauthorizedError('Your session has ended. Please log in again.')
  }
  if (!res.ok) {
    // The API returns validation errors as a plain message.
    const message = (await res.text()).replace(/^"|"$/g, '')
    throw new Error(res.status < 500 && message ? message : `Request failed (${res.status})`)
  }
  return res.status === 204 ? null : res.json()
}

export async function getParks() {
  const data = await request('/parks')
  return data.map(fromApi)
}

// [{ parkId, sitesLeft }] for parks with at least one site that's in season and free every night of the stay.
export function getAvailableParks(checkIn, checkOut) {
  const query = new URLSearchParams({ checkIn, checkOut })
  return request(`/parks/available?${query}`)
}

export function getReservations() {
  return request('/reservations')
}

// Each site in the campground, with `available` false if it's booked for any of those nights.
export function getSites(campgroundId, checkIn, checkOut) {
  const query = new URLSearchParams({ checkIn, checkOut })
  return request(`/campgrounds/${campgroundId}/sites?${query}`)
}

export function createReservation({ siteId, checkIn, checkOut, guests }) {
  return request('/reservations', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ siteId, checkIn, checkOut, guests }),
  })
}

export function cancelReservation(code) {
  return request(`/reservations/${encodeURIComponent(code)}`, { method: 'DELETE' })
}

export function signUp(user) {
  return request('/users', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(user),
  })
}

// Saves the token and returns the user's profile.
export async function logIn({ email, password }) {
  const { accessToken } = await request('/users/login', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ email, password }),
  })
  setToken(accessToken)
  return getMe()
}

export function logOut() {
  setToken(null)
}

export function getMe() {
  return request('/users/me')
}
