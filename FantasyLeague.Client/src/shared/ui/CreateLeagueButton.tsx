import { useApp } from '../../app/AppContext'
import { Icon } from './Icon'

export function CreateLeagueButton() {
  const { texts } = useApp()
  return (
    <button className="create">
      <Icon name="plus" size={18} />
      {texts.actions.createLeague}
    </button>
  )
}
