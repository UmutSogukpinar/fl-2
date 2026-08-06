export interface NbaPlayer {
  id: string
  firstName: string
  lastName: string
  team: string
  position: string
}

export interface PlayerSeasonStats {
  season: number
  gamesPlayed: number
  pointsPerGame: number
  reboundsPerGame: number
  assistsPerGame: number
  stealsPerGame: number
  blocksPerGame: number
  turnoversPerGame: number
  fieldGoalPercentage: number
  threePointPercentage: number
  freeThrowPercentage: number
  minutesPerGame: number
}

export interface NbaPlayerDetails extends NbaPlayer {
  nbaId: number
  jerseyNumber: number | null
  heightCm: number | null
  weightKg: number | null
  createdAt: string
  updatedAt: string | null
  seasonStats: PlayerSeasonStats | null
}
