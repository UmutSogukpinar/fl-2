import { useEffect, useState } from 'react'
import { appEvents } from '../../shared/events'
import { leaguesApi } from './leagues.api'
import type { League } from './types'

export function useLeagues() {
  const [leagues, setLeagues] = useState<League[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    const controller = new AbortController()
    const load = () => {
      setLoading(true)
      setError(null)
      leaguesApi
        .list(controller.signal)
        .then(({ items }) => setLeagues(items))
        .catch((requestError: unknown) => {
          if (requestError instanceof DOMException && requestError.name === 'AbortError') return
          setError(requestError instanceof Error ? requestError.message : 'Failed to load leagues.')
        })
        .finally(() => setLoading(false))
    }

    load()
    window.addEventListener(appEvents.leagueCreated, load)
    return () => {
      controller.abort()
      window.removeEventListener(appEvents.leagueCreated, load)
    }
  }, [])

  return { leagues, loading, error }
}
