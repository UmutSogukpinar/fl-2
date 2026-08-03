import type { LeagueStatus } from '../leagues/types'

export interface DraftPick {
  id: string
  teamId: string
  teamName: string
  round: number
  positionInRound: number
  overallPick: number
  nbaPlayerId?: string | null
  nbaPlayerName?: string | null
  pickedAt?: string | null
}

export interface DraftState {
  leagueId: string
  status: LeagueStatus | number
  completedPicks: number
  totalPicks: number
  currentPick?: DraftPick | null
  pickDeadlineUtc?: string | null
  picks: DraftPick[]
}

export interface MakeDraftPickRequest {
  teamId: string
  ownerId: string
  nbaPlayerId: string
}
