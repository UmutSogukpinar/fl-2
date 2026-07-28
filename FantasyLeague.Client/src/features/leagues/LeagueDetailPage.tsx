import { useEffect, useMemo, useState } from 'react'
import { useApp } from '../../app/AppContext'
import { useCurrentUser } from '../../app/UserContext'
import { draftApi } from '../draft/draft.api'
import { normalizeStatus, statusLabels } from './league.utils'
import { leaguesApi, type FantasyTeam } from './leagues.api'
import type { DraftPickOrder, League, LeagueFixture } from './types'

export function LeagueDetailPage({ leagueId }: { leagueId: string }) {
  const { navigate } = useApp()
  const { userId } = useCurrentUser()
  const [league, setLeague] = useState<League | null>(null)
  const [members, setMembers] = useState<FantasyTeam[]>([])
  const [fixtures, setFixtures] = useState<LeagueFixture[]>([])
  const [draftOrder, setDraftOrder] = useState<DraftPickOrder[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [closing, setClosing] = useState(false)
  const [cancelling, setCancelling] = useState(false)

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
    leaguesApi.fixtures(leagueId, controller.signal)
      .then(setFixtures)
      .catch(() => setFixtures([]))
    leaguesApi.draftOrder(leagueId, controller.signal)
      .then(setDraftOrder)
      .catch(() => setDraftOrder([]))
    return () => controller.abort()
  }, [leagueId])

  const weeks = useMemo(() => fixtures.reduce<Map<number, LeagueFixture[]>>((result, fixture) => {
    const games = result.get(fixture.week) ?? []
    games.push(fixture)
    result.set(fixture.week, games)
    return result
  }, new Map()), [fixtures])
  const myTeam = members.find((member) => member.ownerId === userId)

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

  if (loading) return <section className="workspace-page"><div className="empty">Lig yükleniyor...</div></section>
  if (!league) return <section className="workspace-page"><div className="api-error">{error ?? 'Lig bulunamadı.'}</div></section>
  const status = normalizeStatus(league.status)

  return (
    <section className="workspace-page">
      <button className="text-button" onClick={() => navigate('leagues')}>← Liglere dön</button>
      <div className="league-detail-hero">
        <div><span className={`status status-${status}`}>{statusLabels[status]}</span><h1>{league.name}</h1><p>{league.description}</p></div>
        <div className="league-meta"><span>Katılım kodu<strong>{league.joinCode}</strong></span><span>Takımlar<strong>{members.length}/{league.maxTeams}</strong></span><span>Sezon<strong>{league.season}</strong></span></div>
      </div>
      {error && <div className="api-error" role="alert"><span />{error}</div>}
      <div className="detail-actions">
        {(status === 'Drafting' || status === 'Active') && <button className="create" onClick={() => navigate(`draft/${leagueId}`)}>Draft odasına git</button>}
        {status === 'DraftDelayed' && league.commissionerId === userId && <button className="create" disabled={closing} onClick={closeDelayedLeague}>{closing ? 'Kapatılıyor...' : 'Geciken ligi sonlandır'}</button>}
        {league.commissionerId === userId && ['Created', 'RegistrationOpen', 'DraftDelayed'].includes(status) && <button className="danger-button" disabled={cancelling} onClick={cancelLeague}>{cancelling ? 'İptal ediliyor...' : 'Ligi iptal et'}</button>}
      </div>
      <div className="detail-grid">
        <article className="detail-panel"><h2>Lig üyeleri</h2>{members.map((member) => <div className="member-row" key={member.id}><strong>{member.name}</strong><span>{member.ownerId === userId ? 'Senin takımın' : 'Üye'}</span></div>)}{!members.length && <p>Henüz takım yok.</p>}</article>
        <article className="detail-panel"><h2>Draft sırası</h2>{draftOrder.slice(0, 20).map((pick) => <div className="member-row" key={pick.id}><strong>#{pick.overallPick} {pick.teamName}</strong><span>Tur {pick.round}</span></div>)}{!draftOrder.length && <p>Lig kapanınca snake draft sırası oluşturulur.</p>}</article>
      </div>
      <article className="detail-panel fixtures-panel"><h2>Fikstür</h2>{Array.from(weeks).map(([week, games]) => <div className="fixture-week" key={week}><h3>{week}. Hafta</h3>{games.map((game) => <div className="fixture-row" key={game.id}><span>{game.homeTeamName}</span><strong>vs</strong><span>{game.awayTeamName}</span></div>)}</div>)}{!fixtures.length && <p>Lig kapanınca fikstür oluşturulur.</p>}</article>
      {myTeam && <p className="my-team-note">Bu ligdeki takımın: <strong>{myTeam.name}</strong></p>}
    </section>
  )
}
