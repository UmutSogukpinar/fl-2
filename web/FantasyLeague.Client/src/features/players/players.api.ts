import { apiClient } from '../../shared/api/client'
import type { PagedResponse } from '../../shared/types/api'
import type { NbaPlayer, NbaPlayerDetails } from './types'

export const playersApi = {
  details: (id: string, season = 2024, signal?: AbortSignal) =>
    apiClient<NbaPlayerDetails>(
      `/nba-players/${id}?season=${season}&size=Extended`,
      { signal },
    ),
  list: (pageNumber: number, pageSize: number, signal?: AbortSignal) =>
    apiClient<PagedResponse<NbaPlayer>>(
      `/nba-players?pageNumber=${pageNumber}&pageSize=${pageSize}`,
      { signal },
    ),
  search: (
    name: string,
    surname: string,
    pageNumber: number,
    pageSize: number,
    signal?: AbortSignal,
  ) => {
    const params = new URLSearchParams({
      pageNumber: String(pageNumber),
      pageSize: String(pageSize),
      size: 'basic',
    })

    if (name) params.set('name', name)
    if (surname) params.set('surname', surname)

    return apiClient<PagedResponse<NbaPlayer>>(
      `/nba-players/search?${params.toString()}`,
      { signal },
    )
  },
}
