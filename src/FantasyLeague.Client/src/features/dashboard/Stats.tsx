import { useApp } from '../../app/AppContext'

export function Stats({ total, active }: { total: number; active: number }) {
  const {
    texts: {
      dashboard: { stats },
    },
  } = useApp()

  return (
    <section className="stats">
      <div>
        <small>{stats.totalLeagues}</small>
        <strong>{String(total).padStart(2, '0')}</strong>
        <span>{stats.currentSeason}</span>
      </div>
      <div>
        <small>{stats.activeLeagues}</small>
        <strong>{String(active).padStart(2, '0')}</strong>
        <span className="live">
          <i />
          {stats.live}
        </span>
      </div>
    </section>
  )
}
