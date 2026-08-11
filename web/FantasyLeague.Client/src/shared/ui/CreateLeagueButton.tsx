import { useState } from 'react'
import { useApp } from '../../app/AppContext'
import { CreateLeagueModal } from '../../features/leagues/CreateLeagueModal'
import { Icon } from './Icon'

export function CreateLeagueButton() {
  const { texts } = useApp()
  const [open, setOpen] = useState(false)
  return (
    <>
      <button className="create" onClick={() => setOpen(true)}>
        <Icon name="plus" size={18} />
        {texts.actions.createLeague}
      </button>
      {open && <CreateLeagueModal onClose={() => setOpen(false)} />}
    </>
  )
}
