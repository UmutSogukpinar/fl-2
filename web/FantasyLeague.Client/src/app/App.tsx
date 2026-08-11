import { AppLayout } from '../components/layout/AppLayout'
import { DashboardPage } from '../features/dashboard/DashboardPage'
import { PlayersPage } from '../features/players/PlayersPage'
import { useApp } from './AppContext'
import { useCurrentUser } from './UserContext'
import { UserSetupPage } from '../features/users/UserSetupPage'
import { LeaguesPage } from '../features/leagues/LeaguesPage'
import { LeagueDetailPage } from '../features/leagues/LeagueDetailPage'
import { MyTeamPage } from '../features/leagues/MyTeamPage'
import { DraftPage } from '../features/draft/DraftPage'
import { TransfersPage } from '../features/transfers/TransfersPage'
import { MatchPage } from '../features/matches/MatchPage'

export default function App() {
  const { route } = useApp()
  const { userId, loading } = useCurrentUser()
  if (loading) return <div className="session-loading">Oturum kontrol ediliyor...</div>
  if (!userId) return <UserSetupPage />
  const [section, resourceId, childId] = route.split('/')
  const page = section === 'players' ? <PlayersPage />
    : section === 'leagues' && resourceId ? <LeagueDetailPage leagueId={resourceId} />
    : section === 'leagues' ? <LeaguesPage />
    : section === 'draft' && resourceId ? <DraftPage leagueId={resourceId} />
    : section === 'matches' && resourceId && childId
      ? <MatchPage leagueId={resourceId} fixtureId={childId} />
    : section === 'team' ? <MyTeamPage />
    : section === 'transfers' ? <TransfersPage leagueId={resourceId} />
    : <DashboardPage />

  return (
    <AppLayout>
      {page}
    </AppLayout>
  )
}
