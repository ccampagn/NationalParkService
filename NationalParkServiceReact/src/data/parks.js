// Park data comes from the API (GET /api/parks). This file holds the client-side constants and display helpers.

export const REGIONS = [
  'All',
  'West',
  'Southwest',
  'Rockies',
  'Pacific NW',
  'Midwest',
  'East',
  'Alaska',
  'Islands',
]

export const ACTIVITIES = [
  'Hiking',
  'Camping',
  'Stargazing',
  'Wildlife',
  'Kayaking',
  'Climbing',
  'Lodging',
]

export const RESERVATION_FEE = 8

// Landscape colors are presentation, so they live here rather than in the database.
// Each park gets one of its scene's palettes, chosen by park id.
const SCENE_PALETTES = {
  peaks: [
    { sky: ['#ffcf9f', '#f28b6c'], sun: '#fff1d6', layers: ['#b0677a', '#6d4566', '#2c2a44'] },
    { sky: ['#cfe6ff', '#9cc7e8'], sun: '#ffffff', layers: ['#88a9c9', '#4c6f94', '#1f3550'] },
    { sky: ['#ffe1b8', '#c9a7d8'], sun: '#fff5e6', layers: ['#9b85b5', '#5f4f84', '#2a2448'] },
  ],
  mesa: [
    { sky: ['#ffd88a', '#ff9a5a'], sun: '#fff6dc', layers: ['#d9784a', '#a8462f', '#5e2520'] },
    { sky: ['#bfe3f0', '#f6c79a'], sun: '#fffbe9', layers: ['#e49a6b', '#b8583a', '#4f2a26'] },
    { sky: ['#2b2d5c', '#c86a6a'], sun: '#ffe3c2', layers: ['#b2513a', '#7c2f25', '#3a1a1a'] },
  ],
  coast: [
    { sky: ['#dfe9e6', '#b4cfc9'], sun: '#ffffff', layers: ['#6f9a8f', '#3c6b64', '#1e3f45'] },
    { sky: ['#ffc6a8', '#9fc3e0'], sun: '#fff0e0', layers: ['#6e8fae', '#3f5e7e', '#1c3049'] },
  ],
  forest: [
    { sky: ['#d5ecf5', '#a7d2c4'], sun: '#ffffff', layers: ['#7fa89a', '#3f6f5f', '#1d3b33'] },
    { sky: ['#e8eef0', '#c4d6d0'], sun: '#ffffff', layers: ['#9bb3ad', '#5b7d74', '#26423b'] },
  ],
}

// Shapes an API park into what the components expect.
export function fromApi(p) {
  const palettes = SCENE_PALETTES[p.scene] ?? SCENE_PALETTES.peaks
  return {
    id: p.parkId,
    name: p.parkName,
    state: p.state,
    region: p.region,
    tagline: p.tagline,
    rating: p.rating,
    reviews: p.reviews,
    baseRate: p.baseRate,
    sitesLeft: p.sitesLeft,
    badge: p.badge,
    scene: p.scene,
    palette: palettes[p.parkId % palettes.length],
    activities: p.activities,
    imageUrl: p.imageUrl,
    imageCredit: p.imageCredit,
    imageSourceUrl: p.imageSourceUrl,
  }
}

export function siteOptionsFor(park) {
  return [
    {
      id: 'tent',
      label: 'Tent site',
      detail: 'Fire ring, picnic table, vault toilets',
      rate: park.baseRate,
      capacity: 6,
    },
    {
      id: 'rv',
      label: 'RV hookup',
      detail: 'Water and 50-amp electric, paved pad',
      rate: park.baseRate + 22,
      capacity: 8,
    },
    {
      id: 'cabin',
      label: 'Rustic cabin',
      detail: 'Bunks, heater, shared bathhouse',
      rate: park.baseRate * 3 + 15,
      capacity: 4,
    },
    {
      id: 'backcountry',
      label: 'Backcountry permit',
      detail: 'Wilderness camping, pack in / pack out',
      rate: 10,
      capacity: 4,
    },
  ]
}
