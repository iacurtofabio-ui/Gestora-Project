import { useEffect } from 'react'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { useFasceOrarie } from '@/hooks/useFasceOrarie'
import { useZone } from '@/hooks/useZone'
import { useCreaPrenotazione } from '@/hooks/usePrenotazioni'

const schema = z.object({
    dataPrenotazione: z.string().min(1, 'Data obbligatoria'),
    fasciaOrariaId: z.number().min(1, 'Fascia oraria obbligatoria'),
    zonaId: z.number().nullable().optional(),
    numeroCoperti: z.number().min(1, 'Almeno 1 coperto'),
    note: z.string().optional(),
})

type FormValues = z.infer<typeof schema>

type Props = {
    isOpen: boolean
    onClose: () => void
}

export default function PrenotazioneModal({ isOpen, onClose }: Props) {
    const fasceOrarie = useFasceOrarie()
    const zone = useZone()
    const creaPrenotazione = useCreaPrenotazione()

    const { register, handleSubmit, reset, setValue, formState: { errors } } = useForm<FormValues>({
        resolver: zodResolver(schema),
        defaultValues: {
            dataPrenotazione: '',
            fasciaOrariaId: undefined,
            zonaId: null,
            numeroCoperti: undefined,
            note: '',
        },
    })

    useEffect(() => {
        if (!isOpen) reset()
    }, [isOpen, reset])

    const onSubmit = (values: FormValues) => {
        creaPrenotazione.mutate(
            {
                dataPrenotazione: values.dataPrenotazione,
                fasciaOrariaId: values.fasciaOrariaId,
                zonaId: values.zonaId ?? null,
                numeroCoperti: values.numeroCoperti,
                note: values.note ?? null,
            },
            { onSuccess: onClose }
        )
    }

    if (!isOpen) return null

    return (
        <div className="fixed inset-0 bg-black/40 flex items-center justify-center z-50">
            <div className="bg-white rounded-lg p-6 w-full max-w-md">
                <h2 className="text-lg font-semibold mb-4">Nuova Prenotazione</h2>
                <form onSubmit={handleSubmit(onSubmit)} className="flex flex-col gap-4">

                    <div>
                        <label className="text-sm font-medium">Data</label>
                        <input type="date" {...register('dataPrenotazione')} className="border rounded px-3 py-2 w-full text-sm" />
                        {errors.dataPrenotazione && <p className="text-red-500 text-xs mt-1">{errors.dataPrenotazione.message}</p>}
                    </div>

                    <div>
                        <label className="text-sm font-medium">Fascia Oraria</label>
                        <select {...register('fasciaOrariaId', { valueAsNumber: true })} className="border rounded px-3 py-2 w-full text-sm">
                            <option value="">-- Seleziona --</option>
                            {fasceOrarie.data?.map((f) => (
                                <option key={f.id} value={f.id}>{f.orarioInizio} - {f.orarioFine}</option>
                            ))}
                        </select>
                        {errors.fasciaOrariaId && <p className="text-red-500 text-xs mt-1">{errors.fasciaOrariaId.message}</p>}
                    </div>

                    <div>
                        <label className="text-sm font-medium">Zona (opzionale)</label>
                        <select
                            className="border rounded px-3 py-2 w-full text-sm"
                            onChange={(e) => setValue('zonaId', e.target.value ? Number(e.target.value) : null)}
                        >
                            <option value="">-- Nessuna preferenza --</option>
                            {zone.data?.map((z) => (
                                <option key={z.id} value={z.id}>{z.nome}</option>
                            ))}
                        </select>
                    </div>

                    <div>
                        <label className="text-sm font-medium">Numero Coperti</label>
                        <input type="number" {...register('numeroCoperti', { valueAsNumber: true })} className="border rounded px-3 py-2 w-full text-sm" />
                        {errors.numeroCoperti && <p className="text-red-500 text-xs mt-1">{errors.numeroCoperti.message}</p>}
                    </div>

                    <div>
                        <label className="text-sm font-medium">Note (opzionale)</label>
                        <textarea {...register('note')} className="border rounded px-3 py-2 w-full text-sm" rows={3}
                        />
                    </div>

                    <div className="flex justify-end gap-2">
                        <button type="button" onClick={onClose} className="px-4 py-2 text-sm border rounded">Annulla</button>
                        <button type="submit" className="px-4 py-2 text-sm bg-blue-500 text-white rounded">Salva</button>
                    </div>
                </form>
            </div>
        </div>
    )
}