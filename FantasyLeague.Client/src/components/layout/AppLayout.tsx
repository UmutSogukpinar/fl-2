import type { ReactNode } from 'react'
import { useApp } from '../../app/AppContext'
import { Header } from './Header'
import { Sidebar } from './Sidebar'

export function AppLayout({ children }: { children: ReactNode }) {
  const { sidebarOpen, setSidebarOpen } = useApp()
  return (
    <div className="app-shell">
      <Sidebar />
      {sidebarOpen && <div className="overlay" onClick={() => setSidebarOpen(false)} />}
      <main>
        <Header />
        <div className="content">{children}</div>
      </main>
    </div>
  )
}
