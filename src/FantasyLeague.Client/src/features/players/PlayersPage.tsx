import { useEffect, useState } from 'react'
import { Icon } from '../../shared/ui/Icon'
import { playersApi } from './players.api'
import type { NbaPlayer, NbaPlayerDetails } from './types'
import { usePlayers } from './usePlayers'

export function PlayersPage() {
  const [page, setPage] = useState(1)
  const [name, setName] = useState('')
  const [surname, setSurname] = useState('')
  const [debouncedName, setDebouncedName] = useState('')
  const [debouncedSurname, setDebouncedSurname] = useState('')
  const [selectedPlayer, setSelectedPlayer] = useState<NbaPlayer | null>(null)
  const [playerDetails, setPlayerDetails] = useState<NbaPlayerDetails | null>(null)
  const [detailsLoading, setDetailsLoading] = useState(false)
  const [detailsError, setDetailsError] = useState<string | null>(null)
  const { players, totalCount, totalPages, loading, error } = usePlayers(
    page,
    debouncedName,
    debouncedSurname,
  )

  useEffect(() => {
    const timeout = window.setTimeout(() => {
      setPage(1)
      setDebouncedName(name)
      setDebouncedSurname(surname)
    }, 350)

    return () => window.clearTimeout(timeout)
  }, [name, surname])

  useEffect(() => {
    if (!selectedPlayer) return

    const controller = new AbortController()
    setDetailsLoading(true)
    setDetailsError(null)
    setPlayerDetails(null)

    playersApi.details(selectedPlayer.id, 2024, controller.signal)
      .then(setPlayerDetails)
      .catch((requestError: unknown) => {
        if (requestError instanceof DOMException && requestError.name === 'AbortError') return
        setDetailsError(
          requestError instanceof Error ? requestError.message : 'Oyuncu detayları yüklenemedi.',
        )
      })
      .finally(() => {
        if (!controller.signal.aborted) setDetailsLoading(false)
      })

    return () => controller.abort()
  }, [selectedPlayer])

  useEffect(() => {
    if (!selectedPlayer) return
    const closeOnEscape = (event: KeyboardEvent) => {
      if (event.key === 'Escape') setSelectedPlayer(null)
    }
    window.addEventListener('keydown', closeOnEscape)
    return () => window.removeEventListener('keydown', closeOnEscape)
  }, [selectedPlayer])

  return (
    <section className="players-page">
      <div className="players-heading">
        <div>
          <span>NBA OYUNCULARI</span>
          <h1>Oyuncu havuzu</h1>
          <p>{totalCount} aktif oyuncu</p>
        </div>
      </div>

      <div className="players-search-fields">
        <div className="players-search">
          <Icon name="search" size={18} />
          <input
            type="search"
            value={name}
            onChange={(event) => setName(event.target.value)}
            placeholder="Ada göre ara..."
            aria-label="Oyuncu adına göre ara"
          />
          {name && (
            <button type="button" onClick={() => setName('')} aria-label="Ad aramasını temizle">
              <Icon name="close" size={16} />
            </button>
          )}
        </div>
        <div className="players-search">
          <Icon name="search" size={18} />
          <input
            type="search"
            value={surname}
            onChange={(event) => setSurname(event.target.value)}
            placeholder="Soyada göre ara..."
            aria-label="Oyuncu soyadına göre ara"
          />
          {surname && (
            <button type="button" onClick={() => setSurname('')} aria-label="Soyad aramasını temizle">
              <Icon name="close" size={16} />
            </button>
          )}
        </div>
      </div>

      {error && <div className="api-error" role="alert"><span />{error}</div>}

      {loading ? (
        <div className="players-message player-cards-message">Oyuncular yükleniyor…</div>
      ) : players.length === 0 ? (
        <div className="players-message player-cards-message">Oyuncu bulunamadı.</div>
      ) : (
        <div className="player-card-grid">
          {players.map((player) => (
            <button
              className="player-card"
              type="button"
              key={player.id}
              onClick={() => setSelectedPlayer(player)}
            >
              <span className="player-card-avatar"><Icon name="users" size={22} /></span>
              <span className="player-card-copy">
                <strong>{player.firstName} {player.lastName}</strong>
                <small>{player.team || 'Serbest oyuncu'}</small>
              </span>
              <span className="position-chip">{player.position || '-'}</span>
            </button>
          ))}
        </div>
      )}

      {totalPages > 1 && (
        <div className="pagination">
          <button disabled={page === 1 || loading} onClick={() => setPage((value) => value - 1)}>
            Önceki
          </button>
          <span>{page} / {totalPages}</span>
          <button
            disabled={page === totalPages || loading}
            onClick={() => setPage((value) => value + 1)}
          >
            Sonraki
          </button>
        </div>
      )}

      {selectedPlayer && (
        <PlayerDetailsModal
          player={selectedPlayer}
          details={playerDetails}
          loading={detailsLoading}
          error={detailsError}
          onClose={() => setSelectedPlayer(null)}
        />
      )}
    </section>
  )
}

function PlayerDetailsModal({
  player,
  details,
  loading,
  error,
  onClose,
}: {
  player: NbaPlayer
  details: NbaPlayerDetails | null
  loading: boolean
  error: string | null
  onClose: () => void
}) {
  const stats = details?.seasonStats
  const statItems = stats ? [
    ['Maç', stats.gamesPlayed],
    ['Dakika', stats.minutesPerGame],
    ['Sayı', stats.pointsPerGame],
    ['Ribaund', stats.reboundsPerGame],
    ['Asist', stats.assistsPerGame],
    ['Top çalma', stats.stealsPerGame],
    ['Blok', stats.blocksPerGame],
    ['Top kaybı', stats.turnoversPerGame],
    ['Şut %', stats.fieldGoalPercentage],
    ['Üçlük %', stats.threePointPercentage],
    ['Serbest atış %', stats.freeThrowPercentage],
  ] : []

  return (
    <div className="modal-backdrop" role="presentation" onMouseDown={onClose}>
      <article
        className="player-details-modal"
        role="dialog"
        aria-modal="true"
        aria-labelledby="player-details-title"
        onMouseDown={(event) => event.stopPropagation()}
      >
        <div className="player-details-head">
          <span className="player-details-avatar"><Icon name="users" size={28} /></span>
          <div>
            <span>{details?.team || player.team || 'Serbest oyuncu'}</span>
            <h2 id="player-details-title">{player.firstName} {player.lastName}</h2>
            <p>
              {details?.position || player.position || 'Pozisyon yok'}
              {details?.jerseyNumber != null ? ` · #${details.jerseyNumber}` : ''}
            </p>
          </div>
          <button type="button" onClick={onClose} aria-label="Oyuncu detaylarını kapat">
            <Icon name="close" size={20} />
          </button>
        </div>

        {loading && <div className="player-details-state">İstatistikler yükleniyor…</div>}
        {error && <div className="api-error" role="alert"><span />{error}</div>}
        {!loading && !error && details && (
          <>
            <div className="player-physical-details">
              <span><small>Boy</small><strong>{details.heightCm ? `${details.heightCm} cm` : '-'}</strong></span>
              <span><small>Kilo</small><strong>{details.weightKg ? `${details.weightKg} kg` : '-'}</strong></span>
              <span><small>Sezon</small><strong>{stats?.season ?? 2024}</strong></span>
            </div>
            {stats ? (
              <div className="player-stat-grid">
                {statItems.map(([label, value]) => (
                  <div key={label}><small>{label}</small><strong>{value}</strong></div>
                ))}
              </div>
            ) : (
              <div className="player-details-state">Bu sezon için istatistik bulunamadı.</div>
            )}
          </>
        )}
      </article>
    </div>
  )
}
