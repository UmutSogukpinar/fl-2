import { texts } from '../../shared/constants/texts'
import type { League, LeagueStatus } from './types'

const statuses: LeagueStatus[] = [
  'Created',
  'RegistrationOpen',
  'DraftDelayed',
  'Drafting',
  'DraftCancelled',
  'Active',
  'Completed',
]

export const statusLabels: Record<LeagueStatus, string> = texts.league.statuses

export function normalizeStatus(status: League['status']): LeagueStatus {
  return typeof status === 'number' ? (statuses[status] ?? 'Created') : status
}

export function formatDraftDate(draftDate?: string | null) {
  if (!draftDate) return 'Belirlenmedi'
  return new Date(draftDate).toLocaleString('tr-TR', {
    day: '2-digit',
    month: '2-digit',
    year: 'numeric',
    hour: '2-digit',
    minute: '2-digit',
  })
}
