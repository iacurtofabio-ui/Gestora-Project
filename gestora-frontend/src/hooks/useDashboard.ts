import { useQuery } from '@tanstack/react-query';
import apiClient from '@/lib/axios';
import type { DashboardGiornalieraDTO, DashboardSettimanaleDTO } from '@/types/dashboard';



export function useDashboardGiornaliera(data: string) {
   return useQuery<DashboardGiornalieraDTO>({
      queryKey: ['dashboard-giornaliera', data],  // chiave univoca per la cache
      queryFn: () => apiClient.get('/Dashboard/giornaliera?data=' + data).then(r => r.data),
   })
}

export function useDashboardSettimanale(dataInizio: string) {
   return useQuery<DashboardSettimanaleDTO>({
      queryKey: ['dashboard-settimanale', dataInizio],  // chiave univoca per la cache
      queryFn: () => apiClient.get('/Dashboard/settimanale?dataInizio=' + dataInizio).then(r => r.data),
   })
}


