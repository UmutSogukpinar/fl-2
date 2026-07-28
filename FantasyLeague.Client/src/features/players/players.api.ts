import { apiClient } from '../../shared/api/client'
import type { PagedResponse } from '../../shared/types/api'
import type { NbaPlayer } from './types'

export const playersApi = {
  list: (pageNumber: number, pageSize: number, signal?: AbortSignal) =>
    apiClient<PagedResponse<NbaPlayer>>(
      `/nba-players?pageNumber=${pageNumber}&pageSize=${pageSize}`,
      { signal },
    ),
}
