// Same rules as ReservationsController, which follows Recreation.gov's overnight camping policy. The API has the final say.
import { addDays } from './dates.js'

export const CHANGE_FEE = 10
export const CANCELLATION_FEE = 10

// The change fee for moving `trip` to these dates: charged only when they share no nights with the current ones.
export function changeFeeFor(trip, checkIn, checkOut) {
  return checkIn < trip.checkOut && trip.checkIn < checkOut ? 0 : CHANGE_FEE
}

// Cancelling the day before or the day of arrival.
export function isLateCancellation(trip, today) {
  return trip.checkIn <= addDays(today, 1)
}

// What cancelling `trip` today refunds. The reservation and change fees are never refunded and the cancellation fee is
// kept; a late cancellation also forfeits the first night, or everything for a one-night stay.
export function cancellationRefund(trip, today) {
  const late = isLateCancellation(trip, today)
  if (late && trip.nights === 1) return 0
  const refund = trip.total - trip.reservationFee - trip.changeFees - CANCELLATION_FEE - (late ? trip.nightlyRate : 0)
  return Math.max(refund, 0)
}
