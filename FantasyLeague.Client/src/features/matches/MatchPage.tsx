import { useCallback, useEffect, useState } from 'react'
import { useApp } from '../../app/AppContext'
import { leaguesApi } from '../leagues/leagues.api'
import type { League, LeagueFixture, MatchStats, MatchStatus, TeamMatchStats } from '../leagues/types'

const matchStatuses: MatchStatus[] = [
  'Scheduled', 'InProgress', 'Completed', 'Postponed', 'Cancelled',
]

const matchStatusLabels: Record<MatchStatus, string> = {
  Scheduled: 'Maç planlandı',
  InProgress: 'Maç devam ediyor',
  Completed: 'Maç tamamlandı',
  Postponed: 'Maç ertelendi',
  Cancelled: 'Maç iptal edildi',
}

function normalizeMatchStatus(status: LeagueFixture['status']): MatchStatus {
  return typeof status === 'number'
    ? (matchStatuses[status] ?? 'Scheduled')
    : status
}

const statRows: Array<{ label: string; key: keyof TeamMatchStats; digits?: number }> = [
  { label: 'Oyuncu', key: 'playerCount' },
  { label: 'Sayı', key: 'pointsPerGame', digits: 1 },
  { label: 'Ribaund', key: 'reboundsPerGame', digits: 1 },
  { label: 'Asist', key: 'assistsPerGame', digits: 1 },
  { label: 'Top çalma', key: 'stealsPerGame', digits: 1 },
  { label: 'Blok', key: 'blocksPerGame', digits: 1 },
  { label: 'Top kaybı', key: 'turnoversPerGame', digits: 1 },
  { label: 'Şut yüzdesi', key: 'fieldGoalPercentage', digits: 1 },
  { label: 'Üçlük yüzdesi', key: 'threePointPercentage', digits: 1 },
  { label: 'Serbest atış', key: 'freeThrowPercentage', digits: 1 },
]

function displayStat(stats: TeamMatchStats, key: keyof TeamMatchStats, digits = 0) {
  const value = stats[key]
  return typeof value === 'number' ? value.toFixed(digits) : value
}

export function MatchPage({ leagueId, fixtureId }: { leagueId: string; fixtureId: string }) {
  const { navigate } = useApp()
  const [league, setLeague] = useState<League | null>(null)
  const [fixture, setFixture] = useState<LeagueFixture | null>(null)
  const [stats, setStats] = useState<MatchStats | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  const loadMatch = useCallback(async (signal?: AbortSignal) => {
    const [leagueResponse, fixtures] = await Promise.all([
      leaguesApi.getById(leagueId, signal),
      leaguesApi.fixtures(leagueId, signal),
    ])
    const selected = fixtures.find((item) => String(item.id) === fixtureId)
    if (!selected) throw new Error('Maç bulunamadı.')
    const matchStats = await leaguesApi.matchStats(
      leagueId, selected.homeTeamId, selected.awayTeamId, signal)
    setLeague(leagueResponse)
    setFixture(selected)
    setStats(matchStats)
    setError(null)
  }, [fixtureId, leagueId])

  useEffect(() => {
    const controller = new AbortController()
    loadMatch(controller.signal)
      .catch((requestError: unknown) => {
        if (requestError instanceof DOMException && requestError.name === 'AbortError') return
        setError(requestError instanceof Error ? requestError.message : 'Maç yüklenemedi.')
      })
      .finally(() => setLoading(false))
    const refresh = window.setInterval(() => {
      loadMatch().catch(() => undefined)
    }, 30_000)
    return () => {
      controller.abort()
      window.clearInterval(refresh)
    }
  }, [loadMatch])

  if (loading) return <section className="workspace-page"><div className="empty">Maç yükleniyor...</div></section>
  if (!fixture || !stats) return <section className="workspace-page"><button className="text-button" onClick={() => navigate(`leagues/${leagueId}`)}>← Lige dön</button><div className="api-error">{error ?? 'Maç bulunamadı.'}</div></section>

  const matchStatus = normalizeMatchStatus(fixture.status)
  const completed = matchStatus === 'Completed'
  return (
    <section className="workspace-page match-page">
      <button className="text-button" onClick={() => navigate(`leagues/${leagueId}`)}>← Lig detayına dön</button>
      <div className="match-scoreboard">
        <div className="match-kicker">{league?.name} · {fixture.week}. Hafta</div>
        <div className="match-teams">
          <div><span>EV SAHİBİ</span><h1>{fixture.homeTeamName}</h1></div>
          <strong className="match-score">{completed ? `${fixture.homeScore} : ${fixture.awayScore}` : 'VS'}</strong>
          <div><span>DEPLASMAN</span><h1>{fixture.awayTeamName}</h1></div>
        </div>
        <div className={`match-state ${completed ? 'finished' : ''}`}>
          {matchStatus !== 'Scheduled' ? matchStatusLabels[matchStatus] : fixture.gameTime
            ? `Başlangıç: ${new Date(fixture.gameTime).toLocaleString('tr-TR')}`
            : matchStatusLabels.Scheduled}
        </div>
      </div>
      {error && <div className="api-error">{error}</div>}
      <article className="detail-panel match-stats-panel">
        <div className="match-stat-heading"><strong>{fixture.homeTeamName}</strong><span>TAKIM İSTATİSTİKLERİ</span><strong>{fixture.awayTeamName}</strong></div>
        {statRows.map((row) => (
          <div className="match-stat-row" key={row.key}>
            <strong>{displayStat(stats.homeTeamStats, row.key, row.digits)}</strong>
            <span>{row.label}</span>
            <strong>{displayStat(stats.awayTeamStats, row.key, row.digits)}</strong>
          </div>
        ))}
      </article>
      <p className="match-note">Bu ekran fikstür ve takım istatistiklerini 30 saniyede bir yeniler.</p>
    </section>
  )
}
