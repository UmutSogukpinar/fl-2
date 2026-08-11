import { useEffect, useMemo, useState } from 'react'
import { useApp } from '../../app/AppContext'
import { useCurrentUser } from '../../app/UserContext'
import { Icon } from '../../shared/ui/Icon'
import { leaguesApi, type FantasyTeam } from '../leagues/leagues.api'
import { usePlayers } from '../players/usePlayers'
import { useDraft } from './useDraft'
import { normalizeStatus } from '../leagues/league.utils'

export function DraftPage({ leagueId }: { leagueId: string }) {
  const { navigate } = useApp()
  const { userId } = useCurrentUser()
  const { state, loading, error, socketStatus, makePick } = useDraft(leagueId)
  const [members, setMembers] = useState<FantasyTeam[]>([])
  const [page, setPage] = useState(1)
  const [picking, setPicking] = useState<string | null>(null)
  const [pickError, setPickError] = useState<string | null>(null)
  const [secondsRemaining, setSecondsRemaining] = useState(0)
  const [name, setName] = useState('')
  const [surname, setSurname] = useState('')
  const [debouncedName, setDebouncedName] = useState('')
  const [debouncedSurname, setDebouncedSurname] = useState('')
  const playersState = usePlayers(page, debouncedName, debouncedSurname)

  useEffect(() => {
    const timeout = window.setTimeout(() => {
      setPage(1)
      setDebouncedName(name)
      setDebouncedSurname(surname)
    }, 350)

    return () => window.clearTimeout(timeout)
  }, [name, surname])

  useEffect(() => {
    const controller = new AbortController()
    leaguesApi
      .members(leagueId, controller.signal)
      .then(({ items }) => setMembers(items))
      .catch(() => undefined)
    return () => controller.abort()
  }, [leagueId])

  useEffect(() => {
    const updateCountdown = () => {
      const deadline = state?.pickDeadlineUtc ? new Date(state.pickDeadlineUtc).getTime() : 0
      setSecondsRemaining(Math.max(0, Math.ceil((deadline - Date.now()) / 1_000)))
    }

    updateCountdown()
    const timer = window.setInterval(updateCountdown, 1_000)
    return () => window.clearInterval(timer)
  }, [state?.pickDeadlineUtc])

  const draftIsOpen = state ? normalizeStatus(state.status) === 'Drafting' : false
  const draftIsCancelled = state
    ? normalizeStatus(state.status) === 'DraftCancelled'
    : false
  useEffect(() => {
    if (state && !draftIsOpen && !draftIsCancelled) navigate(`leagues/${leagueId}`)
  }, [draftIsCancelled, draftIsOpen, leagueId, navigate, state])

  const myTeam = members.find((team) => team.ownerId === userId)
  const selectedPlayerIds = useMemo(
    () => new Set(state?.picks.map((pick) => pick.nbaPlayerId).filter(Boolean)),
    [state],
  )
  const isMyTurn = Boolean(myTeam && state?.currentPick?.teamId === myTeam.id)

  async function selectPlayer(nbaPlayerId: string) {
    if (!myTeam || !userId || !isMyTurn) return
    setPicking(nbaPlayerId)
    setPickError(null)
    try {
      await makePick({ teamId: myTeam.id, ownerId: userId, nbaPlayerId })
    } catch (requestError) {
      setPickError(requestError instanceof Error ? requestError.message : 'Oyuncu seçilemedi.')
    } finally {
      setPicking(null)
    }
  }

  if (loading)
    return (
      <section className="workspace-page">
        <div className="empty">Draft yükleniyor...</div>
      </section>
    )
  if (!state)
    return (
      <section className="workspace-page">
        <div className="api-error">{error ?? 'Draft bulunamadı.'}</div>
      </section>
    )
  if (!draftIsOpen)
    return (
      <section className="workspace-page">
        {draftIsCancelled ? (
          <>
            <div className="api-error" role="alert">
              Sistemsel bir hata nedeniyle draft beş ardışık denemeden sonra iptal edildi.
              Lig yöneticisiyle iletişime geçin.
            </div>
            <button className="text-button" onClick={() => navigate(`leagues/${leagueId}`)}>
              Lig detayına dön
            </button>
          </>
        ) : (
          <div className="empty">Draft tamamlandı. Lig detayına yönlendiriliyorsun...</div>
        )}
      </section>
    )

  return (
    <section className="workspace-page draft-page">
      <button className="text-button" onClick={() => navigate(`leagues/${leagueId}`)}>
        ← Lig detayına dön
      </button>
      <div className="draft-header">
        <div>
          <span>CANLI DRAFT</span>
          <h1>
            {state.completedPicks}/{state.totalPicks} seçim tamamlandı
          </h1>
          <p>Socket: {socketStatus}</p>
        </div>
        <div className={isMyTurn ? 'turn-card my-turn' : 'turn-card'}>
          <small>SIRADAKİ SEÇİM</small>
          <strong>
            {state.currentPick
              ? `#${state.currentPick.overallPick} ${state.currentPick.teamName}`
              : 'Draft tamamlandı'}
          </strong>
          {state.currentPick && (
            <div className={secondsRemaining <= 10 ? 'draft-counter urgent' : 'draft-counter'}>
              <b>{secondsRemaining}</b>
              <small>saniye</small>
            </div>
          )}
          <span>{isMyTurn ? 'Seçim sırası sende' : 'Diğer takım bekleniyor'}</span>
        </div>
      </div>
      {(error || pickError) && (
        <div className="api-error" role="alert">
          <span />
          {pickError ?? error}
        </div>
      )}
      <div className="draft-layout">
        <article className="detail-panel draft-board">
          <h2>Seçimler</h2>
          {state.picks.map((pick) => (
            <div
              className={pick.id === state.currentPick?.id ? 'draft-pick current' : 'draft-pick'}
              key={pick.id}
            >
              <span>#{pick.overallPick}</span>
              <strong>{pick.teamName}</strong>
              <em>{pick.nbaPlayerName ?? 'Bekliyor'}</em>
            </div>
          ))}
        </article>
        <article className="detail-panel player-pool">
          <div className="panel-heading">
            <div>
              <h2>Oyuncu havuzu</h2>
              <p>{isMyTurn ? 'Oyuncunu seç' : 'Seçim sırasını bekle'}</p>
            </div>
          </div>
          <div className="players-search-fields draft-player-search">
            <div className="players-search">
              <Icon name="search" size={17} />
              <input
                type="search"
                value={name}
                onChange={(event) => setName(event.target.value)}
                placeholder="Ada göre ara..."
                aria-label="Oyuncu adına göre ara"
              />
              {name && (
                <button type="button" onClick={() => setName('')} aria-label="Ad aramasını temizle">
                  <Icon name="close" size={15} />
                </button>
              )}
            </div>
            <div className="players-search">
              <Icon name="search" size={17} />
              <input
                type="search"
                value={surname}
                onChange={(event) => setSurname(event.target.value)}
                placeholder="Soyada göre ara..."
                aria-label="Oyuncu soyadına göre ara"
              />
              {surname && (
                <button type="button" onClick={() => setSurname('')} aria-label="Soyad aramasını temizle">
                  <Icon name="close" size={15} />
                </button>
              )}
            </div>
          </div>
          {playersState.error && <p className="form-error">{playersState.error}</p>}
          {playersState.loading ? (
            <p>Oyuncular yükleniyor...</p>
          ) : (
            playersState.players.map((player) => {
              const selected = selectedPlayerIds.has(player.id)
              return (
                <div className="draft-player" key={player.id}>
                  <div>
                    <strong>
                      {player.firstName} {player.lastName}
                    </strong>
                    <span>
                      {player.team} · {player.position}
                    </span>
                  </div>
                  <button
                    disabled={!isMyTurn || selected || Boolean(picking)}
                    onClick={() => selectPlayer(player.id)}
                  >
                    {selected ? 'Seçildi' : picking === player.id ? 'Seçiliyor...' : 'Seç'}
                  </button>
                </div>
              )
            })
          )}
          {!playersState.loading && playersState.players.length === 0 && (
            <p>Oyuncu bulunamadı.</p>
          )}
          <div className="pagination">
            <button
              disabled={page === 1 || playersState.loading}
              onClick={() => setPage((value) => value - 1)}
            >
              Önceki
            </button>
            <span>
              {page} / {playersState.totalPages || 1}
            </span>
            <button
              disabled={page >= playersState.totalPages || playersState.loading}
              onClick={() => setPage((value) => value + 1)}
            >
              Sonraki
            </button>
          </div>
        </article>
      </div>
    </section>
  )
}
