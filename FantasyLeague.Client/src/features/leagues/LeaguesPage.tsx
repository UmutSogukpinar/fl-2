import { useState, type FormEvent } from 'react'
import { useCurrentUser } from '../../app/UserContext'
import { CreateLeagueButton } from '../../shared/ui/CreateLeagueButton'
import { LeagueCard } from './LeagueCard'
import { leaguesApi } from './leagues.api'
import { useLeagues } from './useLeagues'

export function LeaguesPage() {
  const { userId } = useCurrentUser()
  const { leagues, loading, error } = useLeagues()
  const [joining, setJoining] = useState(false)
  const [joinError, setJoinError] = useState<string | null>(null)

  async function joinLeague(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    const form = new FormData(event.currentTarget)
    setJoining(true)
    setJoinError(null)
    try {
      const team = await leaguesApi.join(
        String(form.get('joinCode')).trim(),
        String(form.get('teamName')).trim(),
        userId!,
      )
      window.location.hash = `/leagues/${team.leagueId}`
    } catch (requestError) {
      setJoinError(requestError instanceof Error ? requestError.message : 'Lige katılma başarısız oldu.')
    } finally {
      setJoining(false)
    }
  }

  return (
    <section className="workspace-page">
      <div className="page-title-row">
        <div><span>LİGLER</span><h1>Lig merkezi</h1><p>Liglerini yönet veya katılım koduyla yeni bir lige gir.</p></div>
        <CreateLeagueButton />
      </div>
      <form className="join-league-panel" onSubmit={joinLeague}>
        <label>Katılım kodu<input name="joinCode" required /></label>
        <label>Takım adı<input name="teamName" required maxLength={100} /></label>
        <button className="create" disabled={joining}>{joining ? 'Katılınıyor...' : 'Lige katıl'}</button>
        {joinError && <p className="form-error" role="alert">{joinError}</p>}
      </form>
      {error && <div className="api-error" role="alert"><span />{error}</div>}
      {loading ? <div className="empty">Ligler yükleniyor...</div> : (
        <div className="league-grid full-grid">
          {leagues.map((league, index) => <LeagueCard key={league.id} league={league} index={index} />)}
        </div>
      )}
    </section>
  )
}
