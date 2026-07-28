import { useEffect, useState } from 'react'
import { useApp } from '../../app/AppContext'
import { useCurrentUser } from '../../app/UserContext'
import { leaguesApi, type FantasyTeam } from './leagues.api'
import type { League } from './types'

type TeamWithLeague = FantasyTeam & { league: League }

export function MyTeamPage() {
  const { userId } = useCurrentUser()
  const { navigate } = useApp()
  const [teams, setTeams] = useState<TeamWithLeague[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    const controller = new AbortController()
    leaguesApi.list(controller.signal).then(async ({ items }) => {
      const memberPages = await Promise.all(items.map((league) => leaguesApi.members(league.id, controller.signal)))
      setTeams(items.flatMap((league, index) => memberPages[index].items.filter((team) => team.ownerId === userId).map((team) => ({ ...team, league }))))
    }).catch((requestError: unknown) => {
      if (requestError instanceof DOMException && requestError.name === 'AbortError') return
      setError(requestError instanceof Error ? requestError.message : 'Takımlar yüklenemedi.')
    }).finally(() => setLoading(false))
    return () => controller.abort()
  }, [userId])

  return <section className="workspace-page"><div className="page-title-row"><div><span>TAKIMIM</span><h1>Fantasy takımlarım</h1><p>Katıldığın liglerdeki takımların.</p></div></div>{error && <div className="api-error">{error}</div>}{loading ? <div className="empty">Takımlar yükleniyor...</div> : <div className="team-grid">{teams.map((team) => <article className="detail-panel" key={team.id}><span>{team.league.name}</span><h2>{team.name}</h2><button className="text-button" onClick={() => navigate(`leagues/${team.leagueId}`)}>Ligi aç →</button></article>)}{!teams.length && <div className="empty">Henüz bir lig takımın yok.</div>}</div>}</section>
}
