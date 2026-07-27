import { apiClient } from '../../shared/api/client'
import type { PagedResponse } from '../../shared/types/api'
import type { DraftPickOrder, League, LeagueFixture } from './types'

export const leaguesApi = {
  list: (signal?: AbortSignal) =>
    apiClient<PagedResponse<League>>('/leagues?pageNumber=1&pageSize=20', { signal }),
  fixtures: (leagueId: string, signal?: AbortSignal) =>
    apiClient<LeagueFixture[]>(`/leagues/${leagueId}/fixtures`, { signal }),
  draftOrder: (leagueId: string, signal?: AbortSignal) =>
    apiClient<DraftPickOrder[]>(`/leagues/${leagueId}/draft-order`, { signal }),
}
