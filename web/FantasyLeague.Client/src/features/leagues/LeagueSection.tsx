import { useApp } from '../../app/AppContext'
import { CreateLeagueButton } from '../../shared/ui/CreateLeagueButton'
import { Icon } from '../../shared/ui/Icon'
import { LeagueCard } from './LeagueCard'
import type { League } from './types'

export function LeagueSection({ leagues, loading }: { leagues: League[]; loading: boolean }) {
  const {
    texts: { actions, dashboard },
    navigate,
  } = useApp()
  return (
    <section className="leagues-section">
      <div className="section-head">
        <div>
          <span>{dashboard.sectionLabel}</span>
          <h2>{dashboard.sectionTitle}</h2>
        </div>
        <button onClick={() => navigate('leagues')}>
          {actions.viewAll}
          <Icon name="arrow" size={17} />
        </button>
      </div>
      {loading ? (
        <div className="empty" aria-live="polite">
          <p>{dashboard.loading}</p>
        </div>
      ) : leagues.length ? (
        <div className="league-grid">
          {leagues.slice(0, 3).map((league, index) => (
            <LeagueCard key={league.id} league={league} index={index} />
          ))}
        </div>
      ) : (
        <div className="empty">
          <Icon name="trophy" size={34} />
          <h3>{dashboard.emptyTitle}</h3>
          <p>{dashboard.emptyText}</p>
          <CreateLeagueButton />
        </div>
      )}
    </section>
  )
}
