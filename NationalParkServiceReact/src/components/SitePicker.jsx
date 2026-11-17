// Sorts "2" before "10" and "A9" before "A10".
const collator = new Intl.Collator('en', { numeric: true })

// The site tiles in the booking drawer and the trip editor, grouped under their loop (e.g. "Loop A") when sites have one.
// `describe(site)` returns the two small lines under the site number.
export default function SitePicker({ sites, selectedId, fits, onSelect, name, describe }) {
  const sorted = [...sites].sort(
    (a, b) => collator.compare(a.loop ?? '', b.loop ?? '') || collator.compare(a.siteNumber, b.siteNumber),
  )
  const loops = []
  for (const s of sorted) {
    const loop = s.loop ?? ''
    if (loops.at(-1)?.loop !== loop) loops.push({ loop, sites: [] })
    loops.at(-1).sites.push(s)
  }

  return (
    <div className="site-picker" role="radiogroup" aria-label="Site">
      {loops.map(({ loop, sites: loopSites }) => (
        <div key={loop}>
          {loops.length > 1 && <h4 className="site-loop">{loop || 'Other sites'}</h4>}
          <div className="site-grid">
            {loopSites.map((s) => {
              const [line1, line2] = describe(s)
              return (
                <label
                  key={s.siteId}
                  className={`site-tile ${s.siteId === selectedId ? 'selected' : ''}`}
                  aria-disabled={!fits(s)}
                >
                  <input
                    type="radio"
                    name={name}
                    value={s.siteId}
                    checked={s.siteId === selectedId}
                    disabled={!fits(s)}
                    onChange={() => onSelect(s.siteId)}
                  />
                  <strong>{s.siteNumber}</strong>
                  <span>{line1}</span>
                  <span>{line2}</span>
                </label>
              )
            })}
          </div>
        </div>
      ))}
    </div>
  )
}
