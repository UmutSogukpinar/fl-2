import { apiClient } from '../../shared/api/client'
import type { PagedResponse } from '../../shared/types/api'
import type { DraftPickOrder, League, LeagueFixture } from './types'

export interface CreateLeagueRequest {
  name: string
  description?: string | null
  season: number
  maxTeams: number
  commissionerId: string
  draftDate: string
  rosterSize: number
  teamName: string
}

export const leaguesApi = {
  list: (signal?: AbortSignal) =>
    apiClient<PagedResponse<League>>('/leagues?pageNumber=1&pageSize=20', { signal }),
  fixtures: (leagueId: string, signal?: AbortSignal) =>
    apiClient<LeagueFixture[]>(`/leagues/${leagueId}/fixtures`, { signal }),
  draftOrder: (leagueId: string, signal?: AbortSignal) =>
    apiClient<DraftPickOrder[]>(`/leagues/${leagueId}/draft-order`, { signal }),
  create: (request: CreateLeagueRequest) =>
    apiClient<League>('/leagues', {
      method: 'POST',
      body: JSON.stringify(request),
    }),
  getById: (leagueId: string, signal?: AbortSignal) =>
    apiClient<League>(`/leagues/${leagueId}`, { signal }),
  members: (leagueId: string, signal?: AbortSignal) =>
    apiClient<PagedResponse<FantasyTeam>>(
      `/leagues/${leagueId}/members?pageNumber=1&pageSize=100`,
      { signal },
    ),
  join: (joinCode: string, teamName: string, ownerId: string) =>
    apiClient<FantasyTeam>('/leagues/join', {
      method: 'POST',
      body: JSON.stringify({ joinCode, teamName, ownerId }),
    }),
  cancel: (leagueId: string, commissionerId: string) =>
    apiClient<void>(`/leagues/${leagueId}?commissionerId=${commissionerId}`, {
      method: 'DELETE',
    }),
}

export interface FantasyTeam {
  id: string
  name: string
  leagueId: string
  ownerId: string
  createdAt: string
  updatedAt?: string | null
}
