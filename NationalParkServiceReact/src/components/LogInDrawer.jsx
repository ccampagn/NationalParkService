import { useState } from 'react'
import Drawer from './Drawer.jsx'
import { logIn } from '../api.js'

export default function LogInDrawer({ message, onClose, onLogIn, onSwitchToSignUp }) {
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [saving, setSaving] = useState(false)
  const [error, setError] = useState('')

  async function handleSubmit(e) {
    e.preventDefault()
    setSaving(true)
    setError('')
    try {
      const user = await logIn({ email, password })
      onLogIn(user)
    } catch (err) {
      setError(err.message)
      setSaving(false)
    }
  }

  return (
    <Drawer title="Log in" onClose={onClose}>
      <form className="drawer-body signup" onSubmit={handleSubmit}>
        <h2 className="trips-title">Welcome back</h2>
        {message && <p className="empty">{message}</p>}
        <label className="field">
          <span>Email</span>
          <input
            required
            type="email"
            autoComplete="email"
            value={email}
            onChange={(e) => setEmail(e.target.value)}
          />
        </label>
        <label className="field">
          <span>Password</span>
          <input
            required
            type="password"
            autoComplete="current-password"
            value={password}
            onChange={(e) => setPassword(e.target.value)}
          />
        </label>

        {error && <p className="error">{error}</p>}

        <button type="submit" className="btn btn-primary wide" disabled={saving}>
          {saving ? 'Logging in…' : 'Log in'}
        </button>
        <p className="fine">
          New here?{' '}
          <button type="button" className="text-link" onClick={onSwitchToSignUp}>
            Create an account
          </button>
        </p>
      </form>
    </Drawer>
  )
}
