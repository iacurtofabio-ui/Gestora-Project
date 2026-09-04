import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import { RouterProvider } from 'react-router-dom'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { ReactQueryDevtools } from '@tanstack/react-query-devtools'
import { router } from './router/index'
import { AuthProvider } from '@/context/AuthContext'
import ErrorBoundary from '@/components/ErrorBoundary'
import ConfigurazioneMancante from '@/components/ConfigurazioneMancante'
import { configurazioneMancante } from '@/lib/axios'
import './index.css'
import { Toaster } from 'sonner'

const queryClient = new QueryClient()

const radice = createRoot(document.getElementById('root')!)

// REV-017: senza l'indirizzo del backend non ha senso montare l'app - ogni pagina fallirebbe.
// Si mostra invece cosa manca, a schermo e non solo in console.
if (configurazioneMancante) {
  radice.render(<ConfigurazioneMancante />)
} else {
  radice.render(
  <StrictMode>
    {/* REV-014: l'ErrorBoundary sta piu' in alto di AuthProvider, che e' proprio il punto in cui
        un dato di sessione corrotto faceva fallire il primo render lasciando la pagina bianca. */}
    <ErrorBoundary>
      <AuthProvider>
        <QueryClientProvider client={queryClient}>
          <RouterProvider router={router} />
          <ReactQueryDevtools initialIsOpen={false} />
          <Toaster richColors position="top-right" />
        </QueryClientProvider>
      </AuthProvider>
    </ErrorBoundary>
  </StrictMode>
  )
}
