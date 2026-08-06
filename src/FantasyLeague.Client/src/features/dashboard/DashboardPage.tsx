import { useMemo } from 'react'
import { useApp } from '../../app/AppContext'
import { LeagueSection } from '../leagues/LeagueSection'
import { normalizeStatus } from '../leagues/league.utils'
import { useLeagues } from '../leagues/useLeagues'
import { Hero } from './Hero'
import { Stats } from './Stats'

export function DashboardPage() {
  const { texts } = useApp()
  const { leagues, loading, error } = useLeagues()
  const visibleLeagues = useMemo(
    () => leagues.filter((league) => {
      const status = normalizeStatus(league.status)
      return status !== 'Completed' && status !== 'DraftDelayed'
    }),
    [leagues],
  )
  const active = useMemo(
    () => visibleLeagues.filter((league) => normalizeStatus(league.status) === 'Active').length,
    [visibleLeagues],
  )
  return (
    <>
      {error && (
        <div className="api-error" role="alert">
          <span />
          {error}
        </div>
      )}
      <Hero />
      <Stats total={visibleLeagues.length} active={active} />
      <LeagueSection leagues={visibleLeagues} loading={loading} />
    </>
  )
}
