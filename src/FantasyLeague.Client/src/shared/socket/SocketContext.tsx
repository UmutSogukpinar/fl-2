import {
  HubConnectionBuilder,
  HubConnectionState,
  LogLevel,
  type HubConnection,
} from '@microsoft/signalr'
import {
  createContext,
  useContext,
  useEffect,
  useMemo,
  useRef,
  useState,
  type ReactNode,
} from 'react'
import { useCurrentUser } from '../../app/UserContext'

export type SocketStatus = 'connecting' | 'connected' | 'reconnecting' | 'disconnected'

type SocketContextValue = {
  connection: HubConnection
  status: SocketStatus
  connectionId: string | null
}

const HUB_URL = import.meta.env.VITE_HUB_URL ?? '/hubs/fantasy'
const SocketContext = createContext<SocketContextValue | null>(null)

export function SocketProvider({ children }: { children: ReactNode }) {
  const { userId } = useCurrentUser()
  const connectionRef = useRef<HubConnection | null>(null)
  const [status, setStatus] = useState<SocketStatus>('connecting')
  const [connectionId, setConnectionId] = useState<string | null>(null)

  if (!connectionRef.current) {
    connectionRef.current = new HubConnectionBuilder()
      .withUrl(HUB_URL)
      .withAutomaticReconnect([0, 2_000, 5_000, 10_000, 30_000])
      .configureLogging(import.meta.env.DEV ? LogLevel.Information : LogLevel.Warning)
      .build()

    connectionRef.current.on('Connected', (message: { connectionId: string }) => {
      setConnectionId(message.connectionId)
    })
    connectionRef.current.onreconnecting(() => setStatus('reconnecting'))
    connectionRef.current.onreconnected((id) => {
      setConnectionId(id ?? null)
      setStatus('connected')
    })
    connectionRef.current.onclose(() => {
      setConnectionId(null)
      setStatus('disconnected')
    })
  }

  const connection = connectionRef.current

  useEffect(() => {
    let active = true
    let retryTimer: ReturnType<typeof setTimeout> | undefined

    const start = async () => {
      if (!userId) return
      if (connection.state !== HubConnectionState.Disconnected) return
      setStatus('connecting')
      try {
        await connection.start()
        if (active) setStatus('connected')
      } catch (error) {
        if (active) setStatus('disconnected')
        console.error('Failed to establish the socket connection.', error)
        if (active) retryTimer = setTimeout(() => void start(), 5_000)
      }
    }

   
    if (userId) retryTimer = setTimeout(() => void start(), 0)

    return () => {
      active = false
      clearTimeout(retryTimer)
      void connection.stop()
    }
  }, [connection, userId])

  const value = useMemo(
    () => ({ connection, status, connectionId }),
    [connection, connectionId, status],
  )

  return <SocketContext.Provider value={value}>{children}</SocketContext.Provider>
}

export function useSocket() {
  const value = useContext(SocketContext)
  if (!value) throw new Error('useSocket must be used within a SocketProvider.')
  return value
}
