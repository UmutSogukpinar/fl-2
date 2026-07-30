import { useEffect, useState } from 'react'
import { playersApi } from './players.api'
import type { NbaPlayer } from './types'

const PAGE_SIZE = 20

export function usePlayers(pageNumber: number, name = '', surname = '') {
  const [players, setPlayers] = useState<NbaPlayer[]>([])
  const [totalCount, setTotalCount] = useState(0)
  const [totalPages, setTotalPages] = useState(0)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    const controller = new AbortController()
    const normalizedName = name.trim().replace(/\s+/g, ' ')
    const normalizedSurname = surname.trim().replace(/\s+/g, ' ')
    let active = true

    setLoading(true)
    setError(null)

    const request = normalizedName || normalizedSurname
      ? playersApi.search(normalizedName, normalizedSurname, pageNumber, PAGE_SIZE, controller.signal)
      : playersApi.list(pageNumber, PAGE_SIZE, controller.signal)

    request
      .then((response) => {
        if (!active) return
        setPlayers(response.items)
        setTotalCount(response.totalCount)
        setTotalPages(response.totalPages)
      })
      .catch((requestError: unknown) => {
        if (requestError instanceof DOMException && requestError.name === 'AbortError') return
        if (!active) return
        setError(requestError instanceof Error ? requestError.message : 'Failed to load players.')
      })
      .finally(() => {
        if (active) setLoading(false)
      })

    return () => {
      active = false
      controller.abort()
    }
  }, [pageNumber, name, surname])

  return { players, totalCount, totalPages, loading, error }
}
