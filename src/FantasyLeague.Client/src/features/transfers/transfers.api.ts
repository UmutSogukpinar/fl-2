import { apiClient } from '../../shared/api/client'
import type { PagedResponse } from '../../shared/types/api'

export interface RosterPlayer {
  id: string
  firstName: string
  lastName: string
  nbaTeam?: string | null
  position?: string | null
}

export interface TransferPlayer {
  playerId: string
  fromTeamId: string
  firstName: string
  lastName: string
}

export interface Transfer {
  id: string
  initiatingTeamId: string
  counterpartyTeamId: string
  status: 'Pending' | 'Approved' | number
  createdAt: string
  approvedAt?: string | null
  players: TransferPlayer[]
}

export const transfersApi = {
  roster: (teamId: string, signal?: AbortSignal) =>
    apiClient<RosterPlayer[]>(`/fantasy-teams/${teamId}/players`, { signal }),
  playerPool: (teamId: string, pageNumber: number, pageSize: number, signal?: AbortSignal) =>
    apiClient<PagedResponse<RosterPlayer>>(
      `/fantasy-teams/${teamId}/player-pool?pageNumber=${pageNumber}&pageSize=${pageSize}`,
      { signal },
    ),
  addFromPool: (teamId: string, playerId: string) =>
    apiClient<void>(`/fantasy-teams/${teamId}/players/${playerId}`, {
      method: 'POST',
    }),
  release: (teamId: string, playerId: string) =>
    apiClient<void>(`/fantasy-teams/${teamId}?playerId=${playerId}`, {
      method: 'PATCH',
    }),
  list: (teamId: string, signal?: AbortSignal) =>
    apiClient<Transfer[]>(`/fantasy-teams/${teamId}/transfers`, { signal }),
  create: (
    initiatingTeamId: string,
    counterpartyTeamId: string,
    offeredPlayerIds: string[],
    requestedPlayerIds: string[],
  ) => apiClient<{ id: string }>(`/fantasy-teams/${initiatingTeamId}/transfers`, {
    method: 'POST',
    body: JSON.stringify({ counterpartyTeamId, offeredPlayerIds, requestedPlayerIds }),
  }),
  approve: (transferId: string, approvingTeamId: string) =>
    apiClient<void>(`/fantasy-teams/transfers/${transferId}/approve`, {
      method: 'PATCH',
      body: JSON.stringify({ approvingTeamId }),
    }),
}
