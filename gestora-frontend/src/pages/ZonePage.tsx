import { useDeleteZona, useZone } from '@/hooks/useZone'
import { useState } from 'react'
import type { ZonaDTO } from '@/types/zona'
import ZonaModal from '@/components/ZonaModal'
import ConfirmDialog from '@/components/ConfirmDialog'
import { PageLoading, PageError } from '@/components/PageState'
import { EmptyState } from '@/components/EmptyState'
import { Button } from '@/components/ui/button'
import { useAuth } from '@/hooks/useAuth'

export default function ZonePage() {
  const { user } = useAuth()
  const isAdmin = user?.roles.includes('Admin')
  const response = useZone()
  const [isModalOpen, setIsModalOpen] = useState(false)
  const [zonaSelezionata, setZonaSelezionata] = useState<ZonaDTO | undefined>(undefined)
  const deleteZona = useDeleteZona()
  const [idDaEliminare, setIdDaEliminare] = useState<number | undefined>(undefined)

  const numeroColonne = isAdmin ? 3 : 2

  return (
    <div className="bg-white rounded-lg border">
      <div className="flex justify-between items-center p-4 border-b">
        <h2 className="text-sm font-semibold text-gray-700">Zone</h2>
        {isAdmin && (
          <Button
            size="sm"
            onClick={() => {
              setZonaSelezionata(undefined)
              setIsModalOpen(true)
            }}
          >
            + Aggiungi
          </Button>
        )}
      </div>
      {/* REV-074: header e pulsante restano a schermo mentre la tabella carica o va in errore. */}
      {response.isLoading ? (
        <PageLoading />
      ) : response.isError ? (
        <PageError error={response.error} fallback="Errore nel caricamento delle zone." />
      ) : (
        <div className="overflow-x-auto">
          <table className="w-full text-sm">
            <thead>
              <tr className="border-b">
                <th className="text-left p-3">Nome</th>
                <th className="text-left p-3">Attiva</th>
                {isAdmin && <th className="text-left p-3">Azioni</th>}
              </tr>
            </thead>
            <tbody>
              {response.data?.length === 0 ? (
                <EmptyState messaggio="Nessuna zona configurata." colSpan={numeroColonne} />
              ) : (
                response.data?.map((zona) => (
                  <tr key={zona.id} className="border-b">
                    <td className="p-3">{zona.nome}</td>
                    <td className="p-3">{zona.attiva ? 'Sì' : 'No'}</td>
                    {isAdmin && (
                      <td className="p-3 flex gap-2">
                        <Button
                          size="sm"
                          variant="outline"
                          onClick={() => {
                            setZonaSelezionata(zona)
                            setIsModalOpen(true)
                          }}
                        >
                          Modifica
                        </Button>
                        <Button
                          size="sm"
                          variant="destructive"
                          onClick={() => setIdDaEliminare(zona.id)}
                        >
                          Elimina
                        </Button>
                      </td>
                    )}
                  </tr>
                ))
              )}
            </tbody>
          </table>
        </div>
      )}
      <ZonaModal
        isOpen={isModalOpen}
        onClose={() => setIsModalOpen(false)}
        zona={zonaSelezionata}
      />
      <ConfirmDialog
        open={idDaEliminare !== undefined}
        descrizione="Sei sicuro di voler eliminare questa zona? L'operazione non è reversibile."
        onConfirm={() => {
          deleteZona.mutate(idDaEliminare!)
          setIdDaEliminare(undefined)
        }}
        onCancel={() => setIdDaEliminare(undefined)}
      />
    </div>
  )
}
