import { createContext, useContext, useMemo, useState, type ReactNode } from 'react'
import { texts, type AppTexts } from '../shared/constants/texts'

const navigation = [
  { icon: 'grid', label: texts.navigation.overview },
  { icon: 'trophy', label: texts.navigation.leagues },
  { icon: 'users', label: texts.navigation.team },
  { icon: 'chart', label: texts.navigation.players },
  { icon: 'exchange', label: texts.navigation.transfers },
] as const

type AppContextValue = {
  texts: AppTexts
  navigation: typeof navigation
  activeNav: string
  setActiveNav: (value: string) => void
  sidebarOpen: boolean
  setSidebarOpen: (value: boolean) => void
}

const AppContext = createContext<AppContextValue | null>(null)

export function AppProvider({ children }: { children: ReactNode }) {
  const [activeNav, setActiveNav] = useState<string>(navigation[0].label)
  const [sidebarOpen, setSidebarOpen] = useState(false)
  const value = useMemo(
    () => ({ texts, navigation, activeNav, setActiveNav, sidebarOpen, setSidebarOpen }),
    [activeNav, sidebarOpen],
  )
  return <AppContext.Provider value={value}>{children}</AppContext.Provider>
}

export function useApp() {
  const value = useContext(AppContext)
  if (!value) throw new Error('useApp must be used within an AppProvider.')
  return value
}
