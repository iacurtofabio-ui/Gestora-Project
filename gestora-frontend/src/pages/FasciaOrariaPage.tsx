import { useState } from "react";
import type { FasciaOrariaDTO } from '@/types/fasciaOraria';
import { useAllFasceOrarie, useDeleteFasciaOraria } from "@/hooks/useFasceOrarie";
import FasciaOrariaModal from '@/components/FasciaOrariaModal'
import ConfirmDialog from "@/components/ConfirmDialog";
import { useAuth } from '@/hooks/useAuth'

const GIORNI = ['Domenica', 'Lunedì', 'Martedì', 'Mercoledì', 'Giovedì', 'Venerdì', 'Sabato']

export default function FasciaOrariaPage() {
    const { user } = useAuth()
    const isAdmin = user?.roles.includes('Admin')
    // --- hook dati ---
    const { data, isLoading, isError } = useAllFasceOrarie()
    const deleteFasciaOraria = useDeleteFasciaOraria()
    const [idDaEliminare, setIdDaEliminare] = useState<number | undefined>(undefined)

    // --- stato UI ---
    const [isModalOpen, setIsModalOpen] = useState(false)
    const [fasciaSelezionata, setFasciaSelezionata] = useState<FasciaOrariaDTO | undefined>(undefined)

    // --- stati di caricamento ---
    if (isLoading) return <div>Caricamento...</div>
    if (isError) return <div>Errore nel caricamento</div>

    return (
        <div className="bg-white rounded-lg border">
            {/* HEADER */}
            <div className="flex justify-between items-center p-4 border-b">
                <h2 className="text-sm font-semibold text-gray-700">Fasce Orarie</h2>
                {isAdmin && (
                    <button
                        className="bg-blue-500 text-white px-3 py-1 rounded text-sm"
                        onClick={() => { setFasciaSelezionata(undefined); setIsModalOpen(true) }}
                    >
                        + Aggiungi
                    </button>
                )}
            </div>

            {/* TABELLA */}
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
                    {data?.map((fasciaOraria) => (
                        <tr key={fasciaOraria.id} className="border-b">
                            <td className="p-3">{fasciaOraria.orarioInizio}</td>
                            <td className="p-3">{fasciaOraria.orarioFine}</td>
                            <td className="p-3">{GIORNI[fasciaOraria.giornoSettimana]}</td>
                            <td className="p-3">{fasciaOraria.maxCoperti}</td>
                            <td className="p-3">{fasciaOraria.attiva ? 'Sì' : 'No'}</td>

                            {isAdmin && (
                                <td className="p-3 flex gap-2">
                                    <button
                                        className="bg-blue-500 text-white px-3 py-1 rounded text-sm"
                                        onClick={() => { setFasciaSelezionata(fasciaOraria); setIsModalOpen(true) }}
                                    >
                                        Modifica
                                    </button>
                                    <button
                                        className="text-red-500 hover:underline text-sm"
                                        onClick={() => setIdDaEliminare(fasciaOraria.id)}
                                    >
                                        Elimina
                                    </button>
                                </td>
                            )}
                        </tr>
                    ))}
                </tbody>
            </table>
            {/* MODAL */}
            <FasciaOrariaModal
                isOpen={isModalOpen}
                onClose={() => setIsModalOpen(false)}
                fascia={fasciaSelezionata}
            />
            <ConfirmDialog
                open={idDaEliminare !== undefined}
                descrizione="Sei sicuro di voler eliminare questa fascia oraria? L'operazione non è reversibile."
                onConfirm={() => { deleteFasciaOraria.mutate(idDaEliminare!); setIdDaEliminare(undefined) }}
                onCancel={() => setIdDaEliminare(undefined)}
            />
        </div>
    )
}
