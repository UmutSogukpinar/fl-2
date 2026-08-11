import { createContext, useContext, useEffect, useState, type ReactNode } from 'react'
import { usersApi } from '../features/users/users.api'
import type { User } from '../features/users/types'
import { onAuthExpired } from '../shared/api/client'

type UserContextValue = {
  user: User | null
  userId: string | null
  loading: boolean
  setUser: (user: User) => void
  signOut: () => Promise<void>
}

const UserContext = createContext<UserContextValue | null>(null)
let sessionRestoreRequest: Promise<User> | null = null

export function UserProvider({ children }: { children: ReactNode }) {
  const [user, setCurrentUser] = useState<User | null>(null)
  const [loading, setLoading] = useState(true)

  useEffect(() => {
    let active = true
    sessionRestoreRequest ??= usersApi.refresh().finally(() => {
      sessionRestoreRequest = null
    })

    sessionRestoreRequest
      .then((restoredUser) => {
        if (active) setCurrentUser(restoredUser)
      })
      .catch((error: unknown) => {
        if (active) setCurrentUser(null)
      })
      .finally(() => {
        if (active) setLoading(false)
      })

    return () => {
      active = false
    }
  }, [])

  useEffect(() => onAuthExpired(() => setCurrentUser(null)), [])

  const signOut = async () => {
    try {
      await usersApi.signOut()
    } finally {
      setCurrentUser(null)
    }
  }

  return (
    <UserContext.Provider
      value={{ user, userId: user?.id ?? null, loading, setUser: setCurrentUser, signOut }}
    >
      {children}
    </UserContext.Provider>
  )
}

export function useCurrentUser() {
  const value = useContext(UserContext)
  if (!value) throw new Error('useCurrentUser must be used within a UserProvider.')
  return value
}
