import { useId } from 'react'

function treeLine(y, count) {
  const w = 400 / count
  let d = ''
  for (let i = 0; i < count; i++) {
    const x = i * w
    const h = 26 + ((i * 37) % 5) * 7
    d += `M${x - 4} ${y} L${x + w / 2} ${y - h} L${x + w + 4} ${y}Z `
  }
  return d + `M0 ${y - 1} H400 V220 H0Z`
}

const SCENES = {
  peaks: [
    'M0 150 L60 92 L105 128 L170 58 L232 118 L292 72 L350 112 L400 84 V220 H0Z',
    'M0 172 L48 138 L98 160 L158 112 L222 152 L282 122 L340 160 L400 134 V220 H0Z',
    'M0 198 Q100 168 200 188 T400 182 V220 H0Z',
  ],
  mesa: [
    'M0 138 H38 L52 104 H132 L146 138 H228 L238 116 H302 L316 138 H400 V220 H0Z',
    'M0 170 H86 L98 148 H172 L184 170 H258 L268 152 H340 L352 170 H400 V220 H0Z',
    'M0 200 Q120 186 220 196 T400 194 V220 H0Z',
  ],
  coast: [
    'M0 128 L70 96 L120 118 L190 80 L250 112 L320 92 L400 116 V220 H0Z',
    'M0 160 Q90 128 180 152 T400 146 V220 H0Z',
    'M0 186 Q50 180 100 186 T200 186 T300 186 T400 186 V220 H0Z',
  ],
  forest: [
    'M0 140 Q70 100 150 128 T300 116 T400 110 V220 H0Z',
    'M0 168 Q110 140 210 160 T400 150 V220 H0Z',
    treeLine(206, 18),
  ],
}

export default function Landscape({ scene = 'peaks', palette, className, sun = [300, 62] }) {
  const gradientId = useId()
  const layers = SCENES[scene] ?? SCENES.peaks

  return (
    <svg
      className={className}
      viewBox="0 0 400 220"
      preserveAspectRatio="xMidYMax slice"
      aria-hidden="true"
    >
      <defs>
        <linearGradient id={gradientId} x1="0" y1="0" x2="0" y2="1">
          <stop offset="0%" stopColor={palette.sky[0]} />
          <stop offset="100%" stopColor={palette.sky[1]} />
        </linearGradient>
      </defs>
      <rect width="400" height="220" fill={`url(#${gradientId})`} />
      <circle cx={sun[0]} cy={sun[1]} r="24" fill={palette.sun} opacity="0.85" />
      {layers.map((d, i) => (
        <path key={i} d={d} fill={palette.layers[i]} />
      ))}
    </svg>
  )
}
