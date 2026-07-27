import { AppLayout } from '../components/layout/AppLayout'
import { DashboardPage } from '../features/dashboard/DashboardPage'

export default function App() {
  return (
    <AppLayout>
      <DashboardPage />
    </AppLayout>
  )
}
