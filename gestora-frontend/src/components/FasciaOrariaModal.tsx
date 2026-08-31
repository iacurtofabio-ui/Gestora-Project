import { Dialog, DialogContent, DialogHeader, DialogTitle } from "@/components/ui/dialog";
import { useForm } from 'react-hook-form'
import { z } from 'zod'
import { zodResolver } from '@hookform/resolvers/zod'
import { useEffect } from 'react'
import type { FasciaOrariaDTO } from '@/types/fasciaOraria';
import { useCreaFasciaOraria, useUpdateFasciaOraria } from "@/hooks/useFasceOrarie";

const GIORNI = ['Domenica', 'Lunedì',
    'Martedì', 'Mercoledì', 'Giovedì',
    'Venerdì', 'Sabato']

const schema = z.object({
    orarioInizio: z.string().min(1, 'Orario obbligatorio'),
    orarioFine: z.string().min(1, 'Orario obbligatorio'),
    giornoSettimana: z.number().min(0, 'Giorno obbligatorio'),
    maxCoperti: z.number().min(1, 'Numero obbligatorio'),
    attiva: z.boolean(),
})

type FasciaOrariaFormDTO = z.infer<typeof schema>

type Props = {
    isOpen: boolean
    onClose: () => void
    fascia?: FasciaOrariaDTO
}

export default function FasciaOrariaModal({ isOpen, onClose, fascia }: Props) {

    const creaFascia = useCreaFasciaOraria()
    const updateFascia = useUpdateFasciaOraria()

    const { register, handleSubmit, formState: { errors, isSubmitting }, reset, } = useForm<FasciaOrariaFormDTO>({
        resolver: zodResolver(schema),
        defaultValues: {
            orarioInizio: fascia?.orarioInizio,
            orarioFine: fascia?.orarioFine,
            giornoSettimana: fascia?.giornoSettimana,
            maxCoperti: fascia?.maxCoperti,
            attiva: fascia?.attiva ?? true,
        },
    })

    useEffect(() => {
        reset({
            orarioInizio: fascia?.orarioInizio,
            orarioFine: fascia?.orarioFine,
            giornoSettimana: fascia?.giornoSettimana,
            maxCoperti: fascia?.maxCoperti,
            attiva: fascia?.attiva ?? true,
        })
    }, [fascia, reset])

    function onSubmit(data: FasciaOrariaFormDTO) {
        if (fascia) {
            updateFascia.mutate({ ...data, id: fascia.id }, { onSuccess: () => onClose() })
        } else {
            creaFascia.mutate(data, { onSuccess: () => onClose() })
        }
    }

    return (
        <Dialog open={isOpen} onOpenChange={onClose}>
            <DialogContent>
                <DialogHeader>
                    <DialogTitle>{fascia ? 'Modifica Fascia' : 'Nuova Fascia'}</DialogTitle>
                </DialogHeader>
                <form onSubmit={handleSubmit(onSubmit)} className="flex flex-col gap-4">
                    <div>
                        <label className="text-sm font-medium text-gray-700">Ora Inizio</label>
                        <input
                            {...register('orarioInizio')}
                            type="time"
                            placeholder="orarioInizio"
                            className="w-full border rounded px-3 py-2"
                        />
                        {errors.orarioInizio && <p className="text-red-500 text-sm mt-1">{errors.orarioInizio.message}</p>}
                    </div>
                    <div>
                        <label className="text-sm font-medium text-gray-700">Ora Fine</label>
                        <input
                            {...register('orarioFine')}
                            type="time"
                            placeholder="orarioFine"
                            className="w-full border rounded px-3 py-2"
                        />
                        {errors.orarioFine && <p className="text-red-500 text-sm mt-1">{errors.orarioFine.message}</p>}
                    </div>
                    <div>
                        <label className="text-sm font-medium text-gray-700">Giorno Settimana</label>
                        <select {...register('giornoSettimana', {
                            valueAsNumber: true
                        })} className="w-full border rounded px-3 py-2">
                            <option value="">-- Seleziona giorno
                                --</option>
                            {GIORNI.map((nome, indice) => (
                                <option key={indice}
                                    value={indice}>{nome}</option>
                            ))}
                        </select>
                        {errors.giornoSettimana && <p className="text-red-500 text-sm mt-1">{errors.giornoSettimana.message}</p>}
                    </div>
                    <div>
                        <label className="text-sm font-medium text-gray-700">Capienza massima (coperti)</label>
                        <p className="text-xs text-gray-500 mt-0.5">Numero massimo di persone prenotabili in questa fascia oraria, non il numero di prenotazioni.</p>
                        <input
                            {...register('maxCoperti', {
                                valueAsNumber: true
                            })}
                            type="number"
                            placeholder="Es. 40"
                            className="w-full border rounded px-3 py-2 mt-1"
                        />
                        {errors.maxCoperti && <p className="text-red-500 text-sm mt-1">{errors.maxCoperti.message}</p>}
                    </div>
                    <div className="flex items-center gap-2">
                        <input
                            {...register('attiva')}
                            type="checkbox"
                            className="h-4 w-4"
                        />
                        <span>Attiva</span>
                    </div>
                    <button type="submit" className="bg-blue-500 text-white px-3 py-1 rounded" disabled={isSubmitting}>
                        Salva
                    </button>
                </form>
            </DialogContent>
        </Dialog>
    )
}





