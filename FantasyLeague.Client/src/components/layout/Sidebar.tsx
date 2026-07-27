import { useApp } from '../../app/AppContext'
import { Icon } from '../../shared/ui/Icon'

export function Sidebar() {
  const { texts, navigation, activeNav, setActiveNav, sidebarOpen, setSidebarOpen } = useApp()
  return (
    <aside className={sidebarOpen ? 'sidebar open' : 'sidebar'}>
      <button
        className="mobile-close"
        onClick={() => setSidebarOpen(false)}
        aria-label={texts.accessibility.closeMenu}
      >
        <Icon name="close" />
      </button>
      <div className="brand">
        <div className="brand-ball">
          <Icon name="ball" size={25} />
        </div>
        <span>
          {texts.brand.primary}
          <em>{texts.brand.accent}</em>
        </span>
      </div>
      <nav>
        <small>{texts.navigation.title}</small>
        {navigation.map((item) => (
          <button
            key={item.label}
            className={activeNav === item.label ? 'active' : ''}
            onClick={() => {
              setActiveNav(item.label)
              setSidebarOpen(false)
            }}
          >
            <Icon name={item.icon} />
            <span>{item.label}</span>
          </button>
        ))}
      </nav>
    </aside>
  )
}
