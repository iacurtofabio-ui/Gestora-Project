import { useZone } from '@/hooks/useZone'
import { usePostazioni, useDeletePostazione, useRiepilogoSala } from '@/hooks/usePostazioni'
import { useState } from 'react'
import type { PostazioneDTO } from '@/types/postazione'
import PostazioneModal from '@/components/PostazioneModal'
import ConfirmDialog from '@/components/ConfirmDialog'
import { PageLoading, PageError } from '@/components/PageState'
import { EmptyState } from '@/components/EmptyState'
import { Button } from '@/components/ui/button'
import { useAuth } from '@/hooks/useAuth'

export default function PostazionePage() {
  const { user } = useAuth()
  const isAdmin = user?.roles.includes('Admin')
  const isStaffOrAdmin = isAdmin || user?.roles.includes('Staff')
  const riepilogo = useRiepilogoSala({ enabled: !!isStaffOrAdmin })
  const zone = useZone()
  const [zonaSelezionataId, setZonaSelezionataId] = useState<number | undefined>(undefined)
  const [isModalOpen, setIsModalOpen] = useState(false)
  const [postazioneSelezionata, setPostazioneSelezionata] = useState<PostazioneDTO | undefined>(
    undefined
  )
  const deletePostazione = useDeletePostazione()
  const postazioni = usePostazioni(zonaSelezionataId ?? 0, {
    enabled: zonaSelezionataId !== undefined,
  })
  const [idDaEliminare, setIdDaEliminare] = useState<number | undefined>(undefined)

  if (zone.isLoading) return <PageLoading />
  if (zone.isError) return <PageError error={zone.error} fallback="Errore nel caricamento delle zone." />

  const numeroColonne = isAdmin ? 4 : 3

  return (
    <div className="space-y-4">
      {isStaffOrAdmin && riepilogo.data && (
        <div className="bg-white rounded-lg border">
          <div className="p-4 border-b">
            <h2 className="text-sm font-semibold text-gray-700">Riepilogo sala</h2>
          </div>
          <div className="p-4 flex flex-wrap gap-6 text-sm border-b">
            <div>
              <span className="text-gray-500">Tavoli attivi: </span>
              <span className="font-semibold text-gray-800">{riepilogo.data.tavoliAttivi}</span>
            </div>
            <div>
              <span className="text-gray-500">Posti totali: </span>
              <span className="font-semibold text-gray-800">{riepilogo.data.postiTotali}</span>
            </div>
          </div>
          {riepilogo.data.fasce.length > 0 && (
            <div className="overflow-x-auto">
              <table className="w-full text-sm">
                <thead>
                  <tr className="border-b text-gray-500">
                    <th className="text-left p-3 font-medium">Giorno</th>
                    <th className="text-left p-3 font-medium">Orario</th>
                    <th className="text-left p-3 font-medium">Tetto (coperti)</th>
                    <th className="text-left p-3 font-medium">Copertura tavoli</th>
                  </tr>
                </thead>
                <tbody>
                  {riepilogo.data.fasce.map((f) => (
                    <tr key={f.fasciaOrariaId} className="border-b">
                      <td className="p-3 capitalize">{f.giornoSettimana}</td>
                      <td className="p-3">
                        {f.orarioInizio.slice(0, 5)} – {f.orarioFine.slice(0, 5)}
                      </td>
                      <td className="p-3">{f.maxCoperti}</td>
                      <td className="p-3">
                        <span className={f.tettoCoperto ? 'text-green-600' : 'text-amber-600'}>
                          {f.postiTavoli} posti{' '}
                          {f.tettoCoperto ? '· copre il tetto' : '· sotto il tetto'}
                        </span>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
        </div>
      )}
      <div className="bg-white rounded-lg border">
        <div className="flex justify-between items-center p-4 border-b">
          <h2 className="text-sm font-semibold text-gray-700">Postazioni</h2>
          <div className="flex gap-3 items-center">
            <select
              className="border rounded px-3 py-1 text-sm"
              value={zonaSelezionataId ?? ''}
              onChange={(e) =>
                setZonaSelezionataId(e.target.value ? Number(e.target.value) : undefined)
              }
            >
              <option value="">-- Seleziona zona --</option>
              {zone.data?.map((zona) => (
                <option key={zona.id} value={zona.id}>
                  {zona.nome}
                </option>
              ))}
            </select>
            {isAdmin && (
              <Button
                size="sm"
                onClick={() => {
                  setPostazioneSelezionata(undefined)
                  setIsModalOpen(true)
                }}
              >
                + Aggiungi
              </Button>
            )}
          </div>
        </div>
        {zonaSelezionataId && (
          <div className="px-4 py-2 text-sm text-gray-500 border-b">
            Zona:{' '}
            <span className="font-semibold text-gray-700">
              {zone.data?.find((z) => z.id === zonaSelezionataId)?.nome}
            </span>
          </div>
        )}
        {/* REV-073: prima di scegliere una zona la tabella era vuota e sembrava un errore,
            non uno stato d'attesa. */}
        {zonaSelezionataId === undefined ? (
          <div className="p-6 text-center text-sm text-gray-400">
            Seleziona una zona per vedere le sue postazioni.
          </div>
        ) : postazioni.isLoading ? (
          <PageLoading />
        ) : postazioni.isError ? (
          <PageError error={postazioni.error} fallback="Errore nel caricamento delle postazioni." />
        ) : (
          <div className="overflow-x-auto">
            <table className="w-full text-sm">
              <thead>
                <tr className="border-b">
                  <th className="text-left p-3">Numero</th>
                  <th className="text-left p-3">Capienza</th>
                  <th className="text-left p-3">Attiva</th>
                  {isAdmin && <th className="text-left p-3">Azioni</th>}
                </tr>
              </thead>
              <tbody>
                {postazioni.data?.length === 0 ? (
                  <EmptyState
                    messaggio="Nessuna postazione in questa zona."
                    colSpan={numeroColonne}
                  />
                ) : (
                  postazioni.data?.map((postazione) => (
                    <tr key={postazione.id} className="border-b">
                      <td className="p-3">{postazione.numero}</td>
                      <td className="p-3">{postazione.capienzaMassima}</td>
                      <td className="p-3">{postazione.attiva ? 'Sì' : 'No'}</td>
                      {isAdmin && (
                        <td className="p-3 flex gap-2">
                          <Button
                            size="sm"
                            variant="outline"
                            onClick={() => {
                              setPostazioneSelezionata(postazione)
                              setIsModalOpen(true)
                            }}
                          >
                            Modifica
                          </Button>
                          <Button
                            size="sm"
                            variant="destructive"
                            onClick={() => setIdDaEliminare(postazione.id)}
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
        <PostazioneModal
          isOpen={isModalOpen}
          onClose={() => setIsModalOpen(false)}
          postazione={postazioneSelezionata}
        />
        <ConfirmDialog
          open={idDaEliminare !== undefined}
          descrizione="Sei sicuro di voler eliminare questa postazione? L'operazione non è reversibile."
          onConfirm={() => {
            deletePostazione.mutate(idDaEliminare!)
            setIdDaEliminare(undefined)
          }}
          onCancel={() => setIdDaEliminare(undefined)}
        />
      </div>
    </div>
  )
}
