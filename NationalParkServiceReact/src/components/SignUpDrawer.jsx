import { useState } from 'react'
import Drawer from './Drawer.jsx'
import { logIn, signUp } from '../api.js'

const EMPTY_FORM = {
  firstName: '',
  lastName: '',
  email: '',
  password: '',
  addressLine1: '',
  addressLine2: '',
  city: '',
  state: '',
  postalCode: '',
}

export default function SignUpDrawer({ onClose, onSignUp, onSwitchToLogIn }) {
  const [form, setForm] = useState(EMPTY_FORM)
  const [saving, setSaving] = useState(false)
  const [error, setError] = useState('')

  function update(field) {
    return (e) => setForm((f) => ({ ...f, [field]: e.target.value }))
  }

  async function handleSubmit(e) {
    e.preventDefault()
    setSaving(true)
    setError('')
    try {
      // The API validates every field again and rejects emails that already have an account.
      await signUp(form)
      const user = await logIn(form)
      onSignUp(user)
      onClose()
    } catch (err) {
      setError(`Couldn't create your account: ${err.message}`)
    } finally {
      setSaving(false)
    }
  }

  return (
    <Drawer title="Sign up" onClose={onClose}>
      <form className="drawer-body signup" onSubmit={handleSubmit}>
        <h2 className="trips-title">Create an account</h2>

        <h3 className="step">1 · About you</h3>
        <div className="field-row">
          <label className="field">
            <span>First name</span>
            <input required autoComplete="given-name" value={form.firstName} onChange={update('firstName')} />
          </label>
          <label className="field">
            <span>Last name</span>
            <input required autoComplete="family-name" value={form.lastName} onChange={update('lastName')} />
          </label>
        </div>
        <label className="field">
          <span>Email</span>
          <input required type="email" autoComplete="email" value={form.email} onChange={update('email')} />
        </label>
        <label className="field">
          <span>Password</span>
          <input
            required
            type="password"
            minLength={8}
            autoComplete="new-password"
            value={form.password}
            onChange={update('password')}
          />
        </label>

        <h3 className="step">2 · Address</h3>
        <label className="field">
          <span>Street address</span>
          <input required autoComplete="address-line1" value={form.addressLine1} onChange={update('addressLine1')} />
        </label>
        <label className="field">
          <span>Apt, suite (optional)</span>
          <input autoComplete="address-line2" value={form.addressLine2} onChange={update('addressLine2')} />
        </label>
        <label className="field">
          <span>City</span>
          <input required autoComplete="address-level2" value={form.city} onChange={update('city')} />
        </label>
        <div className="field-row">
          <label className="field">
            <span>State</span>
            <input
              required
              maxLength={2}
              pattern="[A-Za-z]{2}"
              title="Two-letter state code, like CA"
              autoComplete="address-level1"
              value={form.state}
              onChange={update('state')}
            />
          </label>
          <label className="field">
            <span>ZIP code</span>
            <input
              required
              inputMode="numeric"
              pattern="\d{5}(-\d{4})?"
              title="5-digit ZIP, or ZIP+4 like 12345-6789"
              autoComplete="postal-code"
              value={form.postalCode}
              onChange={update('postalCode')}
            />
          </label>
        </div>

        {error && <p className="error">{error}</p>}

        <button type="submit" className="btn btn-primary wide" disabled={saving}>
          {saving ? 'Creating account…' : 'Sign up'}
        </button>
        <p className="fine">
          Already have an account?{' '}
          <button type="button" className="text-link" onClick={onSwitchToLogIn}>
            Log in
          </button>
        </p>
      </form>
    </Drawer>
  )
}
