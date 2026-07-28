import { createContext, useContext, useEffect, useMemo, useState, type ReactNode } from 'react'
import { texts, type AppTexts } from '../shared/constants/texts'

const navigation = [
  { icon: 'grid', label: texts.navigation.overview, route: 'overview' },
  { icon: 'trophy', label: texts.navigation.leagues, route: 'leagues' },
  { icon: 'users', label: texts.navigation.team, route: 'team' },
  { icon: 'chart', label: texts.navigation.players, route: 'players' },
  { icon: 'exchange', label: texts.navigation.transfers, route: 'transfers' },
] as const

function readRoute() {
  return window.location.hash.replace(/^#\/?/, '') || 'overview'
}

type AppContextValue = {
  texts: AppTexts
  navigation: typeof navigation
  activeNav: string
  setActiveNav: (value: string) => void
  route: string
  navigate: (route: string) => void
  sidebarOpen: boolean
  setSidebarOpen: (value: boolean) => void
}

const AppContext = createContext<AppContextValue | null>(null)

export function AppProvider({ children }: { children: ReactNode }) {
  const [route, setRoute] = useState(readRoute)
  const [sidebarOpen, setSidebarOpen] = useState(false)
  const navigate = (nextRoute: string) => {
    window.location.hash = `/${nextRoute}`
    setRoute(nextRoute)
  }
  const activeNav = navigation.find((item) =>
    route === item.route ||
    (item.route === 'leagues' && (route.startsWith('leagues/') || route.startsWith('draft/')))
  )?.label ?? navigation[0].label
  const setActiveNav = (label: string) => {
    const item = navigation.find((entry) => entry.label === label)
    if (item) navigate(item.route)
  }

  useEffect(() => {
    const syncRoute = () => setRoute(readRoute())
    window.addEventListener('hashchange', syncRoute)
    return () => window.removeEventListener('hashchange', syncRoute)
  }, [])

  const value = useMemo(
    () => ({ texts, navigation, activeNav, setActiveNav, route, navigate, sidebarOpen, setSidebarOpen }),
    [activeNav, route, sidebarOpen],
  )
  return <AppContext.Provider value={value}>{children}</AppContext.Provider>
}

export function useApp() {
  const value = useContext(AppContext)
  if (!value) throw new Error('useApp must be used within an AppProvider.')
  return value
}
