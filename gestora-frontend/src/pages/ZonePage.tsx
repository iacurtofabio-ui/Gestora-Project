import { useDeleteZona, useZone } from '@/hooks/useZone'
import { useState } from 'react'
import type { ZonaDTO } from '@/types/zona'
import ZonaModal from '@/components/ZonaModal'

export default function ZonePage() {
  
  const response = useZone()
  const [isModalOpen, setIsModalOpen] = useState(false)
  const [zonaSelezionata, setZonaSelezionata] = useState<ZonaDTO | undefined>(undefined)
  const deleteZona = useDeleteZona()

  if (response.isLoading) return <div>Caricamento...</div>
  if (response.isError) return <div>Errore nel caricamento</div>

  return (
    <div className="bg-white rounded-lg border">
      <div className="flex justify-between items-center p-4 border-b">
        <h2 className="text-sm font-semibold text-gray-700">Zone</h2>
        <button className="bg-blue-500 text-white px-3 py-1 rounded text-sm" onClick={() => { setZonaSelezionata(undefined); setIsModalOpen(true) }}>
          + Aggiungi
        </button>
      </div>
      <table className="w-full text-sm">
        <thead>
          <tr className="border-b">
            <th className="text-left p-3">Nome</th>
            <th className="text-left p-3">Attiva</th>
            <th className="text-left p-3">Azioni</th>
          </tr>
        </thead>
        <tbody>
          {response.data?.map((zona) => (
            <tr key={zona.id} className="border-b">
              <td className="p-3">{zona.nome}</td>
              <td className="p-3">{zona.attiva ? 'Sì' : 'No'}</td>
              <td className="p-3 flex gap-2">
                <button
                  className="bg-blue-500 text-white px-3 py-1 rounded text-sm"
                  onClick={() => { setZonaSelezionata(zona); setIsModalOpen(true) }}
                >
                  Modifica
                </button>
                <button className="text-red-500 hover:underline text-sm" onClick={() => deleteZona.mutate(zona.id)}>
                  Elimina
                </button>
              </td>
            </tr>
          ))}
        </tbody>
      </table>
      <ZonaModal
        isOpen={isModalOpen}
        onClose={() => setIsModalOpen(false)}
        zona={zonaSelezionata}
      />
    </div>
  )
}
