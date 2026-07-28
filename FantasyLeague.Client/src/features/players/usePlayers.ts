import { useEffect, useState } from 'react'
import { playersApi } from './players.api'
import type { NbaPlayer } from './types'

const PAGE_SIZE = 20

export function usePlayers(pageNumber: number) {
  const [players, setPlayers] = useState<NbaPlayer[]>([])
  const [totalCount, setTotalCount] = useState(0)
  const [totalPages, setTotalPages] = useState(0)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    const controller = new AbortController()
    setLoading(true)
    setError(null)

    playersApi
      .list(pageNumber, PAGE_SIZE, controller.signal)
      .then((response) => {
        setPlayers(response.items)
        setTotalCount(response.totalCount)
        setTotalPages(response.totalPages)
      })
      .catch((requestError: unknown) => {
        if (requestError instanceof DOMException && requestError.name === 'AbortError') return
        setError(requestError instanceof Error ? requestError.message : 'Failed to load players.')
      })
      .finally(() => setLoading(false))

    return () => controller.abort()
  }, [pageNumber])

  return { players, totalCount, totalPages, loading, error }
}
