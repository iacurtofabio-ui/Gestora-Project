import { useQuery } from '@tanstack/react-query'
import apiClient from '@/lib/axios'
import type { DashboardGiornalieraDTO, DashboardSettimanaleDTO } from '@/types/dashboard'
import { Endpoints } from '@/lib/endpoints'

// FASE 7: aggiornamento automatico. In background (tab non attiva) non ha senso continuare a
// interrogare il server per una pagina che nessuno guarda: refetchIntervalInBackground: false.
const INTERVALLO_AGGIORNAMENTO_MS = 60_000

export function useDashboardGiornaliera(data: string) {
  return useQuery<DashboardGiornalieraDTO>({
    queryKey: ['dashboard-giornaliera', data], // chiave univoca per la cache
    queryFn: () => apiClient.get(Endpoints.dashboard.giornaliera(data)).then((r) => r.data),
    refetchInterval: INTERVALLO_AGGIORNAMENTO_MS,
    refetchIntervalInBackground: false,
  })
}

export function useDashboardSettimanale(dataInizio: string) {
  return useQuery<DashboardSettimanaleDTO>({
    queryKey: ['dashboard-settimanale', dataInizio], // chiave univoca per la cache
    queryFn: () => apiClient.get(Endpoints.dashboard.settimanale(dataInizio)).then((r) => r.data),
    refetchInterval: INTERVALLO_AGGIORNAMENTO_MS,
    refetchIntervalInBackground: false,
  })
}
