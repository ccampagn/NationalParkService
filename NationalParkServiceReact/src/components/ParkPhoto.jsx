import { useState } from 'react'
import Landscape from './Landscape.jsx'

// Shows the park's photo with its credit, falling back to the drawn landscape if there is no photo or it fails to load.
export default function ParkPhoto({ park }) {
  const [failed, setFailed] = useState(false)

  if (!park.imageUrl || failed) {
    return <Landscape scene={park.scene} palette={park.palette} />
  }

  return (
    <>
      <img
        className="park-photo"
        src={park.imageUrl}
        alt={park.name}
        loading="lazy"
        onError={() => setFailed(true)}
      />
      {park.imageCredit && (
        <a
          className="photo-credit"
          href={park.imageSourceUrl}
          target="_blank"
          rel="noopener noreferrer"
          title={`Photo: ${park.imageCredit} (Wikimedia Commons)`}
        >
          Photo: {park.imageCredit}
        </a>
      )}
    </>
  )
}
