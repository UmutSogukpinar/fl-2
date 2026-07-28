import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import App from './app/App'
import { AppProvider } from './app/AppContext'
import { SocketProvider } from './shared/socket/SocketContext'
import { UserProvider } from './app/UserContext'
import './styles.css'

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <UserProvider>
      <SocketProvider>
        <AppProvider>
          <App />
        </AppProvider>
      </SocketProvider>
    </UserProvider>
  </StrictMode>,
)
