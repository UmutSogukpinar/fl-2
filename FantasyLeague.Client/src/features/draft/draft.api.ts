import { apiClient } from '../../shared/api/client'
import type { DraftState, MakeDraftPickRequest } from './types'

export const draftApi = {
  getState: (leagueId: string, signal?: AbortSignal) =>
    apiClient<DraftState>(`/leagues/${leagueId}/draft`, { signal }),
  closeDelayedLeague: (leagueId: string, commissionerId: string) =>
    apiClient<DraftState>(`/leagues/${leagueId}/draft/close`, {
      method: 'POST',
      body: JSON.stringify({ commissionerId }),
    }),
  makePick: (leagueId: string, request: MakeDraftPickRequest) =>
    apiClient<DraftState>(`/leagues/${leagueId}/draft/picks`, {
      method: 'POST',
      body: JSON.stringify(request),
    }),
}
