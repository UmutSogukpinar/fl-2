import { useEffect, useMemo, useState, type Dispatch, type SetStateAction } from 'react'
import { useApp } from '../../app/AppContext'
import { useCurrentUser } from '../../app/UserContext'
import { leaguesApi, type FantasyTeam } from '../leagues/leagues.api'
import { transfersApi, type RosterPlayer, type Transfer } from './transfers.api'

const PLAYER_POOL_PAGE_SIZE = 20

export function TransfersPage({ leagueId }: { leagueId?: string }) {
  const { navigate } = useApp()
  const { userId } = useCurrentUser()
  const [teams, setTeams] = useState<FantasyTeam[]>([])
  const [myRoster, setMyRoster] = useState<RosterPlayer[]>([])
  const [playerPool, setPlayerPool] = useState<RosterPlayer[]>([])
  const [poolPage, setPoolPage] = useState(1)
  const [poolTotalPages, setPoolTotalPages] = useState(0)
  const [otherRoster, setOtherRoster] = useState<RosterPlayer[]>([])
  const [transfers, setTransfers] = useState<Transfer[]>([])
  const [counterpartyId, setCounterpartyId] = useState('')
  const [offered, setOffered] = useState<string[]>([])
  const [requested, setRequested] = useState<string[]>([])
  const [loading, setLoading] = useState(Boolean(leagueId))
  const [submitting, setSubmitting] = useState(false)
  const [approving, setApproving] = useState<string | null>(null)
  const [addingPlayer, setAddingPlayer] = useState<string | null>(null)
  const [releasingPlayer, setReleasingPlayer] = useState<string | null>(null)
  const [rosterSize, setRosterSize] = useState<number | null>(null)
  const [error, setError] = useState<string | null>(null)

  const myTeam = useMemo(() => teams.find((team) => team.ownerId === userId), [teams, userId])
  const teamNames = useMemo(() => new Map(teams.map((team) => [team.id, team.name])), [teams])

  useEffect(() => {
    if (!leagueId) return
    const controller = new AbortController()
    Promise.all([
      leaguesApi.members(leagueId, controller.signal),
      leaguesApi.getById(leagueId, controller.signal),
    ])
      .then(([{ items }, league]) => {
        setTeams(items)
        setRosterSize(league.rosterSize)
      })
      .catch((requestError: unknown) => setError(requestError instanceof Error ? requestError.message : 'Takımlar yüklenemedi.'))
      .finally(() => setLoading(false))
    return () => controller.abort()
  }, [leagueId])

  useEffect(() => {
    if (!myTeam) return
    const controller = new AbortController()
    transfersApi.roster(myTeam.id, controller.signal)
      .then(setMyRoster)
      .catch((requestError: unknown) => {
        if (requestError instanceof DOMException && requestError.name === 'AbortError') return
        setError(requestError instanceof Error ? requestError.message : 'Kadro yüklenemedi.')
      })
    transfersApi.list(myTeam.id, controller.signal)
      .then(setTransfers)
      .catch((requestError: unknown) => {
        if (requestError instanceof DOMException && requestError.name === 'AbortError') return
        setError(requestError instanceof Error ? requestError.message : 'Transfer teklifleri yüklenemedi.')
      })
    return () => controller.abort()
  }, [myTeam])

  useEffect(() => {
    if (!myTeam) return
    const controller = new AbortController()
    transfersApi.playerPool(
      myTeam.id, poolPage, PLAYER_POOL_PAGE_SIZE, controller.signal,
    ).then((response) => {
      setPlayerPool(response.items)
      setPoolTotalPages(response.totalPages)
    }).catch((requestError: unknown) => {
      if (requestError instanceof DOMException && requestError.name === 'AbortError') return
      setError(requestError instanceof Error ? requestError.message : 'Oyuncu havuzu yüklenemedi.')
    })
    return () => controller.abort()
  }, [myTeam, poolPage])

  useEffect(() => {
    setRequested([])
    if (!counterpartyId) return setOtherRoster([])
    const controller = new AbortController()
    transfersApi.roster(counterpartyId, controller.signal)
      .then(setOtherRoster)
      .catch((requestError: unknown) => setError(requestError instanceof Error ? requestError.message : 'Rakip kadro yüklenemedi.'))
    return () => controller.abort()
  }, [counterpartyId])

  const toggle = (id: string, setSelected: Dispatch<SetStateAction<string[]>>) =>
    setSelected((selected) =>
      selected.includes(id)
        ? selected.filter((item) => item !== id)
        : [...selected, id],
    )

  async function createTransfer() {
    if (!myTeam || !counterpartyId) return
    setSubmitting(true)
    setError(null)
    try {
      await transfersApi.create(myTeam.id, counterpartyId, offered, requested)
      setTransfers(await transfersApi.list(myTeam.id))
      setOffered([])
      setRequested([])
    } catch (requestError) {
      setError(requestError instanceof Error ? requestError.message : 'Transfer teklifi oluşturulamadı.')
    } finally {
      setSubmitting(false)
    }
  }

  async function approveTransfer(transferId: string) {
    if (!myTeam) return
    setApproving(transferId)
    setError(null)
    try {
      await transfersApi.approve(transferId, myTeam.id)
      setTransfers(await transfersApi.list(myTeam.id))
      setMyRoster(await transfersApi.roster(myTeam.id))
    } catch (requestError) {
      setError(requestError instanceof Error ? requestError.message : 'Transfer onaylanamadı.')
    } finally {
      setApproving(null)
    }
  }

  async function addPlayerFromPool(playerId: string) {
    if (!myTeam) return
    if (rosterSize !== null && myRoster.length >= rosterSize) {
      setError(`Kadro dolu. En fazla ${rosterSize} oyuncu ekleyebilirsin.`)
      return
    }
    setAddingPlayer(playerId)
    setError(null)
    try {
      await transfersApi.addFromPool(myTeam.id, playerId)
      const [roster, pool] = await Promise.all([
        transfersApi.roster(myTeam.id),
        transfersApi.playerPool(myTeam.id, poolPage, PLAYER_POOL_PAGE_SIZE),
      ])
      setMyRoster(roster)
      setPlayerPool(pool.items)
      setPoolTotalPages(pool.totalPages)
    } catch (requestError) {
      setError(requestError instanceof Error ? requestError.message : 'Oyuncu kadroya eklenemedi.')
    } finally {
      setAddingPlayer(null)
    }
  }

  async function releasePlayer(player: RosterPlayer) {
    if (!myTeam) return
    if (!window.confirm(`${player.firstName} ${player.lastName} kadrodan bırakılsın mı?`)) return

    setReleasingPlayer(player.id)
    setError(null)
    try {
      await transfersApi.release(myTeam.id, player.id)
      const [roster, pool] = await Promise.all([
        transfersApi.roster(myTeam.id),
        transfersApi.playerPool(myTeam.id, poolPage, PLAYER_POOL_PAGE_SIZE),
      ])
      setMyRoster(roster)
      setPlayerPool(pool.items)
      setPoolTotalPages(pool.totalPages)
      setOffered((selected) => selected.filter((id) => id !== player.id))
    } catch (requestError) {
      setError(requestError instanceof Error ? requestError.message : 'Oyuncu kadrodan bırakılamadı.')
    } finally {
      setReleasingPlayer(null)
    }
  }

  if (!leagueId) return <section className="workspace-page"><div className="empty"><h3>Bir lig seç</h3><p>Transfer merkezini lig detayından açabilirsin.</p><button className="create" onClick={() => navigate('leagues')}>Liglere git</button></div></section>
  if (loading) return <section className="workspace-page"><div className="empty">Transfer merkezi yükleniyor...</div></section>
  if (!myTeam) return <section className="workspace-page"><button className="text-button" onClick={() => navigate(`leagues/${leagueId}`)}>← Lige dön</button><div className="empty">Bu ligde bir takımın bulunmuyor.</div></section>

  const isPending = (status: Transfer['status']) => status === 'Pending' || status === 0
  return (
    <section className="workspace-page transfer-page">
      <button className="text-button" onClick={() => navigate(`leagues/${leagueId}`)}>← Lig detayına dön</button>
      <div className="page-title-row"><div><span>TRANSFER MERKEZİ</span><h1>{myTeam.name}</h1><p>Çoklu oyuncu tekliflerini oluştur ve gelen teklifleri yönet.</p></div></div>
      {error && <div className="api-error" role="alert">{error}</div>}
      <article className="detail-panel transfer-builder">
        <div className="panel-heading"><div><h2>Yeni teklif</h2><p>Transfer sonrasında iki kadro da lig roster limitinin altında kalamaz.</p></div></div>
        <label className="transfer-team-select">Takas yapılacak takım<select value={counterpartyId} onChange={(event) => setCounterpartyId(event.target.value)}><option value="">Takım seç</option>{teams.filter((team) => team.id !== myTeam.id).map((team) => <option value={team.id} key={team.id}>{team.name}</option>)}</select></label>
        <div className="transfer-rosters">
          <Roster title="Teklif ettiğin oyuncular" players={myRoster} selected={offered} onToggle={(id) => toggle(id, setOffered)} />
          <Roster title="İstediğin oyuncular" players={otherRoster} selected={requested} onToggle={(id) => toggle(id, setRequested)} emptyText={counterpartyId ? 'Bu takımın kadrosu boş.' : 'Önce bir takım seç.'} />
        </div>
        <div className="transfer-summary"><span>{offered.length} oyuncu veriyorsun</span><b>⇄</b><span>{requested.length} oyuncu alıyorsun</span><button className="create" disabled={submitting || !counterpartyId || !offered.length || !requested.length} onClick={createTransfer}>{submitting ? 'Gönderiliyor...' : 'Teklifi gönder'}</button></div>
      </article>
      <article className="detail-panel">
        <div className="panel-heading">
          <div>
            <h2>Kadrom</h2>
            <p>{myRoster.length}{rosterSize !== null ? ` / ${rosterSize}` : ''} oyuncu</p>
          </div>
        </div>
        <div className="transfer-roster team-roster-list">
          {myRoster.map((player) => (
            <div className="member-row" key={player.id}>
              <span>
                <strong>{player.firstName} {player.lastName}</strong>
                <small>{player.nbaTeam ?? 'FA'} · {player.position ?? '-'}</small>
              </span>
              <button
                className="release-player-button"
                type="button"
                disabled={releasingPlayer !== null}
                onClick={() => releasePlayer(player)}
              >
                {releasingPlayer === player.id ? 'Bırakılıyor...' : 'Oyuncuyu bırak'}
              </button>
            </div>
          ))}
          {!myRoster.length && <p>Kadroda oyuncu bulunmuyor.</p>}
        </div>
      </article>
      <article className="detail-panel">
        <div className="panel-heading"><div><h2>Oyuncu havuzu</h2><p>Bu ligde hiçbir takıma bağlı olmayan oyuncular.</p></div></div>
        <PlayerPool
          players={playerPool}
          addingPlayer={addingPlayer}
          rosterFull={rosterSize !== null && myRoster.length >= rosterSize}
          onAdd={addPlayerFromPool}
        />
        <div className="pagination"><button disabled={poolPage === 1} onClick={() => setPoolPage((page) => page - 1)}>Önceki</button><span>{poolPage} / {poolTotalPages || 1}</span><button disabled={poolPage >= poolTotalPages} onClick={() => setPoolPage((page) => page + 1)}>Sonraki</button></div>
      </article>
      <article className="detail-panel"><h2>Transfer teklifleri</h2>{transfers.map((transfer) => {
        const incoming = transfer.counterpartyTeamId === myTeam.id
        const offeredPlayers = transfer.players.filter((player) => player.fromTeamId === transfer.initiatingTeamId)
        const requestedPlayers = transfer.players.filter((player) => player.fromTeamId === transfer.counterpartyTeamId)
        return <div className="transfer-card" key={transfer.id}><div><strong>{incoming ? teamNames.get(transfer.initiatingTeamId) : teamNames.get(transfer.counterpartyTeamId)}</strong><span>{incoming ? 'Gelen teklif' : 'Gönderilen teklif'} · {new Date(transfer.createdAt).toLocaleDateString('tr-TR')}</span></div><div className="transfer-card-players"><span>{offeredPlayers.map((player) => `${player.firstName} ${player.lastName}`).join(', ')}</span><b>⇄</b><span>{requestedPlayers.map((player) => `${player.firstName} ${player.lastName}`).join(', ')}</span></div><div className="transfer-card-action"><em className={isPending(transfer.status) ? 'pending' : 'approved'}>{isPending(transfer.status) ? 'Bekliyor' : 'Onaylandı'}</em>{incoming && isPending(transfer.status) && <button className="create" disabled={approving === transfer.id} onClick={() => approveTransfer(transfer.id)}>{approving === transfer.id ? 'Onaylanıyor...' : 'Onayla'}</button>}</div></div>
      })}{!transfers.length && <p>Henüz transfer teklifi yok.</p>}</article>
    </section>
  )
}

function PlayerPool({ players, addingPlayer, rosterFull, onAdd }: { players: RosterPlayer[]; addingPlayer: string | null; rosterFull: boolean; onAdd: (id: string) => void }) {
  return <div className="transfer-roster">{rosterFull && <p className="form-warning">Kadro dolu. Yeni oyuncu eklemek için önce bir oyuncu bırakmalısın.</p>}{players.map((player) => <div className="member-row" key={player.id}><span><strong>{player.firstName} {player.lastName}</strong><small>{player.nbaTeam ?? 'FA'} · {player.position ?? '-'}</small></span><button className="create" disabled={addingPlayer !== null || rosterFull} onClick={() => onAdd(player.id)}>{addingPlayer === player.id ? 'Ekleniyor...' : rosterFull ? 'Kadro dolu' : 'Kadroya ekle'}</button></div>)}{!players.length && <p>Havuzda uygun oyuncu bulunmuyor.</p>}</div>
}

function Roster({ title, players, selected, onToggle, emptyText = 'Kadro boş.' }: { title: string; players: RosterPlayer[]; selected: string[]; onToggle: (id: string) => void; emptyText?: string }) {
  return <div className="transfer-roster"><h3>{title}<span>{selected.length} seçili</span></h3>{players.map((player) => <label className={selected.includes(player.id) ? 'selected' : ''} key={player.id}><input type="checkbox" checked={selected.includes(player.id)} onChange={() => onToggle(player.id)} /><span><strong>{player.firstName} {player.lastName}</strong><small>{player.nbaTeam ?? 'FA'} · {player.position ?? '-'}</small></span></label>)}{!players.length && <p>{emptyText}</p>}</div>
}
