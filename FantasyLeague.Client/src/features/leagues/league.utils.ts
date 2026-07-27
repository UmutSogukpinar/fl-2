import { texts } from '../../shared/constants/texts'
import type { League, LeagueStatus } from './types'

const statuses: LeagueStatus[] = [
  'Created',
  'RegistrationOpen',
  'DraftDelayed',
  'Drafting',
  'Active',
  'Completed',
]

export const statusLabels: Record<LeagueStatus, string> = texts.league.statuses

export function normalizeStatus(status: League['status']): LeagueStatus {
  return typeof status === 'number' ? (statuses[status] ?? 'Created') : status
}
