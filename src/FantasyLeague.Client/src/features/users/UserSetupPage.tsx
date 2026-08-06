import { useState, type FormEvent } from 'react'
import { useCurrentUser } from '../../app/UserContext'
import { usersApi } from './users.api'

type Mode = 'sign-in' | 'create'

export function UserSetupPage() {
  const { setUser } = useCurrentUser()
  const [mode, setMode] = useState<Mode>('sign-in')
  const [submitting, setSubmitting] = useState(false)
  const [error, setError] = useState<string | null>(null)

  function changeMode(nextMode: Mode) {
    setMode(nextMode)
    setError(null)
  }

  async function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    const form = new FormData(event.currentTarget)
    setSubmitting(true)
    setError(null)

    try {
      const email = String(form.get('email'))
      const password = String(form.get('password'))
      let user
      if (mode === 'sign-in') {
        user = await usersApi.signIn(email, password)
      } else {
        await usersApi.create({
            username: String(form.get('username')),
            email,
            password,
            location: String(form.get('location')),
          })
        user = await usersApi.signIn(email, password)
      }
      setUser(user)
      window.location.hash = '/overview'
    } catch (requestError) {
      setError(requestError instanceof Error ? requestError.message : 'The request failed.')
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <main className="user-setup">
      <section className="user-setup-card">
        <span>HOOPBASE</span>
        <h1>{mode === 'sign-in' ? 'Giriş yap' : 'Profilini oluştur'}</h1>
        <p>
          {mode === 'sign-in'
            ? 'Mevcut profilinle liglerine devam et.'
            : 'Liglerini ve takımını bu profil üzerinden yöneteceksin.'}
        </p>

        <div className="auth-tabs">
          <button
            type="button"
            className={mode === 'sign-in' ? 'active' : ''}
            onClick={() => changeMode('sign-in')}
          >
            Giriş yap
          </button>
          <button
            type="button"
            className={mode === 'create' ? 'active' : ''}
            onClick={() => changeMode('create')}
          >
            Profil oluştur
          </button>
        </div>

        <form onSubmit={submit}>
          {mode === 'create' && (
            <label>Kullanıcı adı<input name="username" required minLength={3} maxLength={50} /></label>
          )}
          <label>E-posta<input name="email" type="email" required /></label>
          <label>Şifre<input name="password" type="password" required minLength={8} /></label>
          {mode === 'create' && (
            <label>Konum<input name="location" defaultValue="Istanbul" required /></label>
          )}
          {error && <p className="form-error" role="alert">{error}</p>}
          <button className="create" type="submit" disabled={submitting}>
            {submitting
              ? 'İşleniyor…'
              : mode === 'sign-in' ? 'Giriş yap' : 'Profil oluştur'}
          </button>
        </form>
      </section>
    </main>
  )
}
