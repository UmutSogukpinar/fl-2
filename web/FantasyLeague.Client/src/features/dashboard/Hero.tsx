import { useApp } from '../../app/AppContext'
import { Icon } from '../../shared/ui/Icon'

export function Hero() {
  const {
    texts: { dashboard },
  } = useApp()
  return (
    <section className="hero">
      <div>
        <div className="eyebrow">
          <i />
          {dashboard.eyebrow}
        </div>
        <h1>
          {dashboard.greeting}
          <span>.</span>
        </h1>
        <p>{dashboard.subtitle}</p>
      </div>
      <div className="hero-art">
        <div className="court">
          <i />
          <i />
          <i />
        </div>
        <div className="floating-ball">
          <Icon name="ball" size={76} />
        </div>
      </div>
    </section>
  )
}
