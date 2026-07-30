import { useEffect, useState } from 'react'
import { Icon } from '../../shared/ui/Icon'
import { usePlayers } from './usePlayers'

export function PlayersPage() {
  const [page, setPage] = useState(1)
  const [name, setName] = useState('')
  const [surname, setSurname] = useState('')
  const [debouncedName, setDebouncedName] = useState('')
  const [debouncedSurname, setDebouncedSurname] = useState('')
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

      <div className="players-table-wrap">
        <table className="players-table">
          <thead>
            <tr>
              <th>Oyuncu</th>
              <th>Takım</th>
              <th>Pozisyon</th>
            </tr>
          </thead>
          <tbody>
            {loading ? (
              <tr><td colSpan={3} className="players-message">Oyuncular yükleniyor…</td></tr>
            ) : players.length === 0 ? (
              <tr><td colSpan={3} className="players-message">Oyuncu bulunamadı.</td></tr>
            ) : players.map((player) => (
              <tr key={player.id}>
                <td>
                  <div className="player-name">
                    <span><Icon name="users" size={17} /></span>
                    <strong>{player.firstName} {player.lastName}</strong>
                  </div>
                </td>
                <td>{player.team}</td>
                <td><span className="position-chip">{player.position}</span></td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>

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
    </section>
  )
}
