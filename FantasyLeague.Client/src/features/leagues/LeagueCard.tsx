import { useApp } from '../../app/AppContext'
import { Icon } from '../../shared/ui/Icon'
import { formatDraftDate, normalizeStatus, statusLabels } from './league.utils'
import type { League } from './types'

export function LeagueCard({ league, index }: { league: League; index: number }) {
  const { texts, navigate } = useApp()
  const status = normalizeStatus(league.status)

  return (
    <article
      className="league-card"
      style={{ '--delay': `${index * 80}ms` } as React.CSSProperties}
    >
      <div className={`card-mark mark-${index % 3}`}>
        <Icon name={index % 2 ? 'ball' : 'trophy'} size={24} />
      </div>
      <div className="card-top">
        <span className={`status status-${status}`}>{statusLabels[status]}</span>
        <button className="more" aria-label={texts.accessibility.leagueMenu}>
          •••
        </button>
      </div>
      <div className="league-season">
        {league.season} {texts.league.seasonSuffix}
      </div>
      <h3>{league.name}</h3>
      <p>{league.description || texts.league.defaultDescription}</p>
      <div className="league-draft-date">
        <span>Draft zamanı</span>
        <strong>{formatDraftDate(league.draftDate)}</strong>
      </div>
      <div className="card-footer">
        <button onClick={() => navigate(`leagues/${league.id}`)}>
          {texts.actions.details}
          <Icon name="arrow" size={16} />
        </button>
      </div>
    </article>
  )
}
