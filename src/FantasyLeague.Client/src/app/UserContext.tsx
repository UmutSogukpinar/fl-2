import { createContext, useContext, useEffect, useState, type ReactNode } from 'react'
import { usersApi } from '../features/users/users.api'
import type { User } from '../features/users/types'

const COOKIE_NAME = 'fantasy_user_id'
const GUID_PATTERN = /^[0-9a-f]{8}-[0-9a-f]{4}-[1-5][0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$/i

function readUserId() {
  const cookie = document.cookie
    .split('; ')
    .find((value) => value.startsWith(`${COOKIE_NAME}=`))
  return cookie ? decodeURIComponent(cookie.split('=')[1]) : null
}

function removeUserCookie() {
  document.cookie = `${COOKIE_NAME}=; Max-Age=0; Path=/; SameSite=Lax`
}

type UserContextValue = {
  user: User | null
  userId: string | null
  loading: boolean
  setUser: (user: User) => void
}

const UserContext = createContext<UserContextValue | null>(null)

export function UserProvider({ children }: { children: ReactNode }) {
  const [user, setCurrentUser] = useState<User | null>(null)
  const [loading, setLoading] = useState(true)

  useEffect(() => {
    const controller = new AbortController()
    const userId = readUserId()

    if (!userId || !GUID_PATTERN.test(userId)) {
      if (userId) removeUserCookie()
      setLoading(false)
      return () => controller.abort()
    }

    usersApi
      .getById(userId, controller.signal)
      .then(setCurrentUser)
      .catch((error: unknown) => {
        if (error instanceof DOMException && error.name === 'AbortError') return
        removeUserCookie()
        setCurrentUser(null)
      })
      .finally(() => setLoading(false))

    return () => controller.abort()
  }, [])

  const setUser = (nextUser: User) => {
    document.cookie = `${COOKIE_NAME}=${encodeURIComponent(nextUser.id)}; Max-Age=31536000; Path=/; SameSite=Lax`
    setCurrentUser(nextUser)
  }

  return (
    <UserContext.Provider value={{ user, userId: user?.id ?? null, loading, setUser }}>
      {children}
    </UserContext.Provider>
  )
}

export function useCurrentUser() {
  const value = useContext(UserContext)
  if (!value) throw new Error('useCurrentUser must be used within a UserProvider.')
  return value
}
