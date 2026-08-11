import { useCallback, useEffect, useState } from 'react'
import { useSocket } from '../../shared/socket/SocketContext'
import { draftApi } from './draft.api'
import type { DraftState, MakeDraftPickRequest } from './types'

const draftEvents = [
  'DraftStarted',
  'DraftUpdated',
  'DraftCompleted',
  'DraftCancelled',
  'LeagueClosed',
] as const

export function useDraft(leagueId: string) {
  const { connection, status: socketStatus } = useSocket()
  const [state, setState] = useState<DraftState | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    const controller = new AbortController()
    setLoading(true)
    setError(null)
    draftApi
      .getState(leagueId, controller.signal)
      .then(setState)
      .catch((requestError: unknown) => {
        if (requestError instanceof DOMException && requestError.name === 'AbortError') return
        setError(requestError instanceof Error ? requestError.message : 'Failed to load the draft.')
      })
      .finally(() => setLoading(false))
    return () => controller.abort()
  }, [leagueId])

  useEffect(() => {
    const updateState = (nextState: DraftState) => setState(nextState)
    draftEvents.forEach((eventName) => connection.on(eventName, updateState))

    return () => {
      draftEvents.forEach((eventName) => connection.off(eventName, updateState))
      if (connection.state === 'Connected') void connection.invoke('LeaveLeague', leagueId)
    }
  }, [connection, leagueId])

  useEffect(() => {
    if (socketStatus === 'connected') void connection.invoke('JoinLeague', leagueId)
  }, [connection, leagueId, socketStatus])

  const makePick = useCallback(async (request: MakeDraftPickRequest) => {
    const nextState = await draftApi.makePick(leagueId, request)
    setState(nextState)
    return nextState
  }, [leagueId])

  const closeDelayedLeague = useCallback(async (commissionerId: string) => {
    const nextState = await draftApi.closeDelayedLeague(leagueId, commissionerId)
    setState(nextState)
    return nextState
  }, [leagueId])

  return { state, loading, error, socketStatus, makePick, closeDelayedLeague }
}
