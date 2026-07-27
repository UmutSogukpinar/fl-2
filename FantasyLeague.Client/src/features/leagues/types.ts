export type LeagueStatus =
  'Created' | 'RegistrationOpen' | 'DraftDelayed' | 'Drafting' | 'Active' | 'Completed'

export interface League {
  id: string
  name: string
  description?: string | null
  season: number
  maxTeams: number
  commissionerId: string
  status: LeagueStatus | number
  draftDate?: string | null
  joinCode: string
  createdAt: string
  updatedAt?: string | null
  rosterSize: number
  draftTimeZoneId: string
}

export interface LeagueFixture {
  id: string
  leagueId: string
  week: number
  homeTeamId: string
  homeTeamName: string
  awayTeamId: string
  awayTeamName: string
}

export interface DraftPickOrder {
  id: string
  leagueId: string
  teamId: string
  teamName: string
  round: number
  positionInRound: number
  overallPick: number
}
