import { Dialog, DialogContent, DialogHeader, DialogTitle } from '@/components/ui/dialog'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { useForm } from 'react-hook-form'
import { z } from 'zod'
import { zodResolver } from '@hookform/resolvers/zod'
import { useEffect } from 'react'
import type { FasciaOrariaDTO } from '@/types/fasciaOraria'
import { useCreaFasciaOraria, useUpdateFasciaOraria } from '@/hooks/useFasceOrarie'
import { GIORNI_SETTIMANA } from '@/lib/giorni'

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

  const {
    register,
    handleSubmit,
    formState: { errors },
    reset,
  } = useForm<FasciaOrariaFormDTO>({
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

  // REV-044: il pulsante segue lo stato della mutation, non isSubmitting di react-hook-form.
  // Quest'ultimo torna false appena onSubmit ritorna, e onSubmit lancia mutate() senza attenderla:
  // il pulsante si riabilitava mentre la richiesta era ancora in volo, quindi un secondo clic
  // partiva davvero e creava un doppione.
  const inCorso = creaFascia.isPending || updateFascia.isPending

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
          <div className="space-y-1">
            <Label htmlFor="fascia-inizio">Ora Inizio</Label>
            <Input id="fascia-inizio" {...register('orarioInizio')} type="time" />
            {errors.orarioInizio && (
              <p className="text-red-500 text-sm mt-1">{errors.orarioInizio.message}</p>
            )}
          </div>
          <div className="space-y-1">
            <Label htmlFor="fascia-fine">Ora Fine</Label>
            <Input id="fascia-fine" {...register('orarioFine')} type="time" />
            {errors.orarioFine && (
              <p className="text-red-500 text-sm mt-1">{errors.orarioFine.message}</p>
            )}
          </div>
          <div className="space-y-1">
            <Label htmlFor="fascia-giorno">Giorno Settimana</Label>
            <select
              id="fascia-giorno"
              {...register('giornoSettimana', {
                valueAsNumber: true,
              })}
              className="w-full border rounded px-3 py-2 text-sm"
            >
              <option value="">-- Seleziona giorno --</option>
              {GIORNI_SETTIMANA.map((nome, indice) => (
                <option key={indice} value={indice}>
                  {nome}
                </option>
              ))}
            </select>
            {errors.giornoSettimana && (
              <p className="text-red-500 text-sm mt-1">{errors.giornoSettimana.message}</p>
            )}
          </div>
          <div className="space-y-1">
            <Label htmlFor="fascia-max-coperti">Capienza massima (coperti)</Label>
            <p className="text-xs text-gray-500">
              Numero massimo di persone prenotabili in questa fascia oraria, non il numero di
              prenotazioni.
            </p>
            <Input
              id="fascia-max-coperti"
              {...register('maxCoperti', {
                valueAsNumber: true,
              })}
              type="number"
              placeholder="Es. 40"
            />
            {errors.maxCoperti && (
              <p className="text-red-500 text-sm mt-1">{errors.maxCoperti.message}</p>
            )}
          </div>
          <div className="flex items-center gap-2">
            <input id="fascia-attiva" {...register('attiva')} type="checkbox" className="h-4 w-4" />
            <Label htmlFor="fascia-attiva">Attiva</Label>
          </div>
          <Button type="submit" disabled={inCorso}>
            {inCorso ? 'Salvataggio...' : 'Salva'}
          </Button>
        </form>
      </DialogContent>
    </Dialog>
  )
}
