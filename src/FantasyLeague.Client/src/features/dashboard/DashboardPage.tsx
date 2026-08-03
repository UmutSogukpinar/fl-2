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
  const active = useMemo(
    () => leagues.filter((x) => normalizeStatus(x.status) === 'Active').length,
    [leagues],
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
      <Stats total={leagues.length} active={active} />
      <LeagueSection leagues={leagues} loading={loading} />
    </>
  )
}
