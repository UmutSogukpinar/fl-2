export type LeagueStatus =
  'Created' | 'RegistrationOpen' | 'DraftDelayed' | 'Drafting' | 'Active' | 'Completed'

export type MatchStatus =
  'Scheduled' | 'InProgress' | 'Completed' | 'Postponed' | 'Cancelled'

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
  homeScore?: number | null
  awayScore?: number | null
  gameTime?: string | null
  status: MatchStatus | number
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

export interface TeamMatchStats {
  fantasyTeamId: string
  season: number
  playerCount: number
  gamesPlayed: number
  gamesStarted: number
  minutesPerGame: number
  pointsPerGame: number
  reboundsPerGame: number
  assistsPerGame: number
  stealsPerGame: number
  blocksPerGame: number
  turnoversPerGame: number
  fieldGoalPercentage: number
  threePointPercentage: number
  freeThrowPercentage: number
}

export interface MatchStats {
  homeTeamStats: TeamMatchStats
  awayTeamStats: TeamMatchStats
}

export interface LeagueStanding {
  position: number
  teamId: string
  teamName: string
  played: number
  won: number
  drawn: number
  lost: number
  pointsFor: number
  pointsAgainst: number
  pointDifference: number
  points: number
}
