import { useState } from 'react'
import type { FasciaOrariaDTO } from '@/types/fasciaOraria'
import { useAllFasceOrarie, useDeleteFasciaOraria } from '@/hooks/useFasceOrarie'
import FasciaOrariaModal from '@/components/FasciaOrariaModal'
import ConfirmDialog from '@/components/ConfirmDialog'
import { PageLoading, PageError } from '@/components/PageState'
import { EmptyState } from '@/components/EmptyState'
import { Button } from '@/components/ui/button'
import { useAuth } from '@/hooks/useAuth'
import { GIORNI_SETTIMANA } from '@/lib/giorni'

export default function FasciaOrariaPage() {
  const { user } = useAuth()
  const isAdmin = user?.roles.includes('Admin')
  // --- hook dati ---
  const { data, isLoading, isError, error } = useAllFasceOrarie()
  const deleteFasciaOraria = useDeleteFasciaOraria()
  const [idDaEliminare, setIdDaEliminare] = useState<number | undefined>(undefined)

  // --- stato UI ---
  const [isModalOpen, setIsModalOpen] = useState(false)
  const [fasciaSelezionata, setFasciaSelezionata] = useState<FasciaOrariaDTO | undefined>(undefined)

  const numeroColonne = isAdmin ? 6 : 5

  return (
    <div className="bg-white rounded-lg border">
      {/* HEADER */}
      <div className="flex justify-between items-center p-4 border-b">
        <h2 className="text-sm font-semibold text-gray-700">Fasce Orarie</h2>
        {isAdmin && (
          <Button
            size="sm"
            onClick={() => {
              setFasciaSelezionata(undefined)
              setIsModalOpen(true)
            }}
          >
            + Aggiungi
          </Button>
        )}
      </div>

      {isLoading ? (
        <PageLoading />
      ) : isError ? (
        <PageError error={error} fallback="Errore nel caricamento delle fasce orarie." />
      ) : (
        <div className="overflow-x-auto">
          <table className="w-full text-sm">
            <thead>
              <tr className="border-b">
                <th className="text-left p-3">Orario Inizio</th>
                <th className="text-left p-3">Orario Fine</th>
                <th className="text-left p-3">Giorno Settimana</th>
                <th className="text-left p-3">Capienza (coperti)</th>
                <th className="text-left p-3">Attiva</th>
                {isAdmin && <th className="text-left p-3">Azioni</th>}
              </tr>
            </thead>
            <tbody>
              {data?.length === 0 ? (
                <EmptyState messaggio="Nessuna fascia oraria configurata." colSpan={numeroColonne} />
              ) : (
                data?.map((fasciaOraria) => (
                  <tr key={fasciaOraria.id} className="border-b">
                    <td className="p-3">{fasciaOraria.orarioInizio}</td>
                    <td className="p-3">{fasciaOraria.orarioFine}</td>
                    <td className="p-3">{GIORNI_SETTIMANA[fasciaOraria.giornoSettimana]}</td>
                    <td className="p-3">{fasciaOraria.maxCoperti}</td>
                    <td className="p-3">{fasciaOraria.attiva ? 'Sì' : 'No'}</td>

                    {isAdmin && (
                      <td className="p-3 flex gap-2">
                        <Button
                          size="sm"
                          variant="outline"
                          onClick={() => {
                            setFasciaSelezionata(fasciaOraria)
                            setIsModalOpen(true)
                          }}
                        >
                          Modifica
                        </Button>
                        <Button
                          size="sm"
                          variant="destructive"
                          onClick={() => setIdDaEliminare(fasciaOraria.id)}
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
      {/* MODAL */}
      <FasciaOrariaModal
        isOpen={isModalOpen}
        onClose={() => setIsModalOpen(false)}
        fascia={fasciaSelezionata}
      />
      <ConfirmDialog
        open={idDaEliminare !== undefined}
        descrizione="Sei sicuro di voler eliminare questa fascia oraria? L'operazione non è reversibile."
        onConfirm={() => {
          deleteFasciaOraria.mutate(idDaEliminare!)
          setIdDaEliminare(undefined)
        }}
        onCancel={() => setIdDaEliminare(undefined)}
      />
    </div>
  )
}
