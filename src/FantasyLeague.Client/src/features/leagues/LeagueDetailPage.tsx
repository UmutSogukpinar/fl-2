import { useEffect, useMemo, useState, type FormEvent } from 'react'
import { useApp } from '../../app/AppContext'
import { useCurrentUser } from '../../app/UserContext'
import { draftApi } from '../draft/draft.api'
import { formatDraftDate, normalizeStatus, statusLabels } from './league.utils'
import { leaguesApi, type FantasyTeam } from './leagues.api'
import type { DraftPickOrder, League, LeagueFixture, LeagueStanding } from './types'

export function LeagueDetailPage({ leagueId }: { leagueId: string }) {
  const { navigate } = useApp()
  const { userId } = useCurrentUser()
  const [league, setLeague] = useState<League | null>(null)
  const [members, setMembers] = useState<FantasyTeam[]>([])
  const [membersLoading, setMembersLoading] = useState(true)
  const [fixtures, setFixtures] = useState<LeagueFixture[]>([])
  const [draftOrder, setDraftOrder] = useState<DraftPickOrder[]>([])
  const [standings, setStandings] = useState<LeagueStanding[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [closing, setClosing] = useState(false)
  const [cancelling, setCancelling] = useState(false)
  const [showJoinForm, setShowJoinForm] = useState(false)
  const [joining, setJoining] = useState(false)

  useEffect(() => {
    const controller = new AbortController()
    setLoading(true)
    leaguesApi.getById(leagueId, controller.signal)
      .then(setLeague)
      .catch((requestError: unknown) => {
        if (requestError instanceof DOMException && requestError.name === 'AbortError') return
        setError(requestError instanceof Error ? requestError.message : 'Lig yüklenemedi.')
      })
      .finally(() => setLoading(false))

    leaguesApi.members(leagueId, controller.signal)
      .then(({ items }) => setMembers(items))
      .catch(() => setMembers([]))
      .finally(() => setMembersLoading(false))
    leaguesApi.fixtures(leagueId, controller.signal)
      .then(setFixtures)
      .catch(() => setFixtures([]))
    leaguesApi.draftOrder(leagueId, controller.signal)
      .then(setDraftOrder)
      .catch(() => setDraftOrder([]))
    leaguesApi.standings(leagueId, controller.signal)
      .then(setStandings)
      .catch(() => setStandings([]))
    return () => controller.abort()
  }, [leagueId])

  const weeks = useMemo(() => fixtures.reduce<Map<number, LeagueFixture[]>>((result, fixture) => {
    const games = result.get(fixture.week) ?? []
    games.push(fixture)
    result.set(fixture.week, games)
    return result
  }, new Map()), [fixtures])
  const myTeam = members.find((member) => member.ownerId === userId)
  const formatGameTime = (gameTime?: string | null) =>
    gameTime
      ? new Date(gameTime).toLocaleString('tr-TR', {
          day: '2-digit',
          month: '2-digit',
          hour: '2-digit',
          minute: '2-digit',
        })
      : 'Saat bekleniyor'

  async function closeDelayedLeague() {
    if (!userId) return
    setClosing(true)
    setError(null)
    try {
      await draftApi.closeDelayedLeague(leagueId, userId)
      setLeague((current) => current ? { ...current, status: 'Completed' } : current)
    } catch (requestError) {
      setError(requestError instanceof Error ? requestError.message : 'Lig kapatılamadı.')
    } finally {
      setClosing(false)
    }
  }

  async function cancelLeague() {
    if (!userId || !window.confirm('Bu ligi iptal etmek istediğine emin misin?')) return
    setCancelling(true)
    setError(null)
    try {
      await leaguesApi.cancel(leagueId, userId)
      navigate('leagues')
    } catch (requestError) {
      setError(requestError instanceof Error ? requestError.message : 'Lig iptal edilemedi.')
      setCancelling(false)
    }
  }

  async function joinLeague(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    if (!userId || !league) return

    const form = new FormData(event.currentTarget)
    const teamName = String(form.get('teamName')).trim()
    setJoining(true)
    setError(null)
    try {
      const team = await leaguesApi.join(league.joinCode, teamName, userId)
      setMembers((current) => [...current, team])
      setShowJoinForm(false)
    } catch (requestError) {
      setError(requestError instanceof Error ? requestError.message : 'Lige katılma başarısız oldu.')
    } finally {
      setJoining(false)
    }
  }

  if (loading) return <section className="workspace-page"><div className="empty">Lig yükleniyor...</div></section>
  if (!league) return <section className="workspace-page"><div className="api-error">{error ?? 'Lig bulunamadı.'}</div></section>
  const status = normalizeStatus(league.status)

  return (
    <section className="workspace-page">
      <button className="text-button" onClick={() => navigate('leagues')}>← Liglere dön</button>
      <div className="league-detail-hero">
        <div><span className={`status status-${status}`}>{statusLabels[status]}</span><h1>{league.name}</h1><p>{league.description}</p></div>
        <div className="league-meta"><span>Katılım kodu<strong>{league.joinCode}</strong></span><span>Takımlar<strong>{members.length}/{league.maxTeams}</strong></span><span>Sezon<strong>{league.season}</strong></span><span>Draft zamanı<strong>{formatDraftDate(league.draftDate)}</strong></span></div>
      </div>
      {error && <div className="api-error" role="alert"><span />{error}</div>}
      <div className="detail-actions">
        {!membersLoading && !myTeam && !showJoinForm && (
          <button className="create" onClick={() => setShowJoinForm(true)}>Lige katıl</button>
        )}
        {status === 'Drafting' && <button className="create" onClick={() => navigate(`draft/${leagueId}`)}>Draft odasına git</button>}
        {myTeam && status === 'Active' && <button className="create" onClick={() => navigate(`transfers/${leagueId}`)}>Transfer merkezine git</button>}
        {status === 'DraftDelayed' && league.commissionerId === userId && <button className="create" disabled={closing} onClick={closeDelayedLeague}>{closing ? 'Kapatılıyor...' : 'Geciken ligi sonlandır'}</button>}
        {league.commissionerId === userId && ['Created', 'RegistrationOpen', 'DraftDelayed'].includes(status) && <button className="danger-button" disabled={cancelling} onClick={cancelLeague}>{cancelling ? 'İptal ediliyor...' : 'Ligi iptal et'}</button>}
      </div>
      {!membersLoading && !myTeam && showJoinForm && (
        <form className="join-league-panel detail-join-panel" onSubmit={joinLeague}>
          <label>
            Takım adı
            <input name="teamName" required maxLength={100} autoFocus />
          </label>
          <button className="create" disabled={joining}>
            {joining ? 'Katılınıyor...' : 'Katıl'}
          </button>
          <button
            className="text-button"
            type="button"
            disabled={joining}
            onClick={() => setShowJoinForm(false)}
          >
            Vazgeç
          </button>
        </form>
      )}
      <div className="detail-grid">
        <article className="detail-panel"><h2>Lig üyeleri</h2>{members.map((member) => <div className="member-row" key={member.id}><strong>{member.name}</strong><span>{member.ownerId === userId ? 'Senin takımın' : 'Üye'}</span></div>)}{!members.length && <p>Henüz takım yok.</p>}</article>
        <article className="detail-panel"><h2>Draft sırası</h2>{draftOrder.slice(0, 20).map((pick) => <div className="member-row" key={pick.id}><strong>#{pick.overallPick} {pick.teamName}</strong><span>Tur {pick.round}</span></div>)}{!draftOrder.length && <p>Lig kapanınca snake draft sırası oluşturulur.</p>}</article>
      </div>
      <article className="detail-panel standings-panel">
        <h2>Puan durumu</h2>
        <div className="standings-row standings-head"><span>#</span><strong>Takım</strong><span>O</span><span>G</span><span>B</span><span>M</span><span>AV</span><b>P</b></div>
        {standings.map((row) => <div className="standings-row" key={row.teamId}><span>{row.position}</span><strong>{row.teamName}</strong><span>{row.played}</span><span>{row.won}</span><span>{row.drawn}</span><span>{row.lost}</span><span>{row.pointDifference > 0 ? `+${row.pointDifference}` : row.pointDifference}</span><b>{row.points}</b></div>)}
        {!standings.length && <p>Henüz puan durumu oluşturulmadı.</p>}
      </article>
      <article className="detail-panel fixtures-panel"><h2>Fikstür</h2>{Array.from(weeks).map(([week, games]) => <div className="fixture-week" key={week}><h3>{week}. Hafta</h3>{games.map((game) => <button className="fixture-row fixture-link" key={game.id} onClick={() => navigate(`matches/${leagueId}/${game.id}`)}><span>{game.homeTeamName}</span><strong className="fixture-time"><b>{game.status === 'Completed' ? `${game.homeScore} - ${game.awayScore}` : 'vs'}</b><small>{formatGameTime(game.gameTime)}</small></strong><span>{game.awayTeamName}</span></button>)}</div>)}{!fixtures.length && <p>Lig kapanınca fikstür oluşturulur.</p>}</article>
      {myTeam && <p className="my-team-note">Bu ligdeki takımın: <strong>{myTeam.name}</strong></p>}
    </section>
  )
}
