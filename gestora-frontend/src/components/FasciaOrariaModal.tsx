import { Dialog, DialogContent, DialogHeader, DialogTitle } from '@/components/ui/dialog'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { NativeSelect } from '@/components/ui/native-select'
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
          <DialogTitle className="text-titolo">{fascia ? 'Modifica fascia oraria' : 'Nuova fascia oraria'}</DialogTitle>
        </DialogHeader>
        <form onSubmit={handleSubmit(onSubmit)} className="flex flex-col gap-4">
          <div className="space-y-1">
            <Label htmlFor="fascia-inizio">Ora di inizio</Label>
            <Input id="fascia-inizio" {...register('orarioInizio')} type="time" />
            {errors.orarioInizio && (
              <p className="text-nota text-destructive">{errors.orarioInizio.message}</p>
            )}
          </div>
          <div className="space-y-1">
            <Label htmlFor="fascia-fine">Ora di fine</Label>
            <Input id="fascia-fine" {...register('orarioFine')} type="time" />
            {errors.orarioFine && (
              <p className="text-nota text-destructive">{errors.orarioFine.message}</p>
            )}
          </div>
          <div className="space-y-1">
            <Label htmlFor="fascia-giorno">Giorno della settimana</Label>
            <NativeSelect
              id="fascia-giorno"
              {...register('giornoSettimana', {
                valueAsNumber: true,
              })}
            >
              <option value="">Scegli un giorno</option>
              {GIORNI_SETTIMANA.map((nome, indice) => (
                <option key={indice} value={indice}>
                  {nome}
                </option>
              ))}
            </NativeSelect>
            {errors.giornoSettimana && (
              <p className="text-nota text-destructive">{errors.giornoSettimana.message}</p>
            )}
          </div>
          <div className="space-y-1">
            <Label htmlFor="fascia-max-coperti">Capienza massima (coperti)</Label>
            <p className="text-nota text-muted-foreground">
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
              <p className="text-nota text-destructive">{errors.maxCoperti.message}</p>
            )}
          </div>
          <div className="flex items-center gap-2">
            <input
              id="fascia-attiva"
              {...register('attiva')}
              type="checkbox"
              className="h-4 w-4 accent-primary"
            />
            <Label htmlFor="fascia-attiva">Attiva</Label>
          </div>
          {/* Stesso piede di tutti gli altri modali: una riga di separazione, la rinuncia in
              chiaro e l'azione in pieno. Prima qui il pulsante di conferma era da solo e a
              tutta larghezza, senza modo di uscire se non con la X in alto. */}
          <div className="flex justify-end gap-2 border-t pt-4">
            <Button type="button" variant="ghost" onClick={onClose}>
              Annulla
            </Button>
            <Button type="submit" disabled={inCorso}>
              {inCorso ? 'Salvataggio…' : fascia ? 'Salva modifiche' : 'Crea fascia'}
            </Button>
          </div>
        </form>
      </DialogContent>
    </Dialog>
  )
}
