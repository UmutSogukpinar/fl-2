import { useApp } from '../../app/AppContext'
import { Icon } from '../../shared/ui/Icon'

export function Header() {
  const { texts, activeNav, setSidebarOpen } = useApp()
  return (
    <header>
      <button
        className="mobile-menu"
        onClick={() => setSidebarOpen(true)}
        aria-label={texts.accessibility.openMenu}
      >
        <Icon name="menu" />
      </button>
      <div className="breadcrumb">
        <span>{texts.brand.breadcrumb}</span>
        <b>/</b>
        <strong>{activeNav}</strong>
      </div>
      <div className="header-actions">
        <button className="notification" aria-label={texts.accessibility.notifications}>
          <Icon name="bell" />
          <i />
        </button>
      </div>
    </header>
  )
}
