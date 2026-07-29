import { useState, type FormEvent } from 'react'
import { appEvents } from '../../shared/events'
import { Icon } from '../../shared/ui/Icon'
import { useCurrentUser } from '../../app/UserContext'
import { leaguesApi } from './leagues.api'

const DEMO_SEASON = 2024

type Props = {
  onClose: () => void
}

function tomorrowAtEight() {
  const value = new Date()
  value.setDate(value.getDate() + 1)
  value.setHours(20, 0, 0, 0)
  const local = new Date(value.getTime() - value.getTimezoneOffset() * 60_000)
  return local.toISOString().slice(0, 16)
}

export function CreateLeagueModal({ onClose }: Props) {
  const { userId } = useCurrentUser()
  const [submitting, setSubmitting] = useState(false)
  const [error, setError] = useState<string | null>(null)

  async function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    const form = new FormData(event.currentTarget)
    setSubmitting(true)
    setError(null)

    try {
      await leaguesApi.create({
        name: String(form.get('name')),
        description: String(form.get('description')) || null,
        season: DEMO_SEASON,
        maxTeams: Number(form.get('maxTeams')),
        commissionerId: userId!,
        draftDate: String(form.get('draftDate')),
        rosterSize: Number(form.get('rosterSize')),
        teamName: String(form.get('teamName')),
      })
      window.dispatchEvent(new Event(appEvents.leagueCreated))
      onClose()
    } catch (requestError) {
      setError(requestError instanceof Error ? requestError.message : 'Failed to create league.')
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <div className="modal-backdrop" role="presentation" onMouseDown={onClose}>
      <section
        className="league-modal"
        role="dialog"
        aria-modal="true"
        aria-labelledby="create-league-title"
        onMouseDown={(event) => event.stopPropagation()}
      >
        <div className="modal-head">
          <div>
            <span>YENİ REKABET</span>
            <h2 id="create-league-title">Lig oluştur</h2>
          </div>
          <button type="button" onClick={onClose} aria-label="Kapat"><Icon name="close" /></button>
        </div>

        <form onSubmit={submit}>
          <label>Lig adı<input name="name" required maxLength={100} /></label>
          <label>Takımın<input name="teamName" required maxLength={100} /></label>
          <label>Açıklama<textarea name="description" maxLength={500} rows={3} /></label>
          <div className="form-grid">
            <label>Demo sezonu<input name="season" type="number" value={DEMO_SEASON} readOnly /></label>
            <label>Maksimum takım<input name="maxTeams" type="number" min="2" max="30" defaultValue={10} required /></label>
            <label>Kadro büyüklüğü<input name="rosterSize" type="number" min="1" max="30" defaultValue={13} required /></label>
            <label>Draft zamanı<input name="draftDate" type="datetime-local" defaultValue={tomorrowAtEight()} required /></label>
          </div>
          {error && <p className="form-error" role="alert">{error}</p>}

          <div className="modal-actions">
            <button type="button" className="secondary" onClick={onClose}>Vazgeç</button>
            <button type="submit" className="create" disabled={submitting}>
              {submitting ? 'Oluşturuluyor…' : 'Ligi oluştur'}
            </button>
          </div>
        </form>
      </section>
    </div>
  )
}
