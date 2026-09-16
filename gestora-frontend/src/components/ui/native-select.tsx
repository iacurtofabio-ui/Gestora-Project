import * as React from 'react'
import { ChevronDownIcon } from 'lucide-react'
import { cn } from '@/lib/utils'

/**
 * Menu a tendina nativo, vestito come gli altri campi del form.
 *
 * Perché non il `Select` di Radix, che pure è installato e si usa nei filtri delle pagine:
 *
 * 1. **Sui form è legato a react-hook-form.** Il `<select>` nativo funziona con un semplice
 *    `{...register('campo')}`. Radix non espone un campo vero, quindi ogni menu andrebbe avvolto
 *    in un `Controller` — più codice per lo stesso risultato.
 * 2. **Sul telefono il menu nativo è migliore.** Apre la rotella di sistema, che si usa con il
 *    pollice; Radix disegna un elenco proprio, che su schermi piccoli è più scomodo.
 * 3. **Nei test è affidabile.** `userEvent.selectOptions` funziona solo su un `<select>` vero.
 *    Radix in ambiente di test ha bisogno di sostituire a mano funzioni del browser che jsdom non
 *    implementa, ed è una fonte nota di test instabili — vedi i 4 test di `PrenotazioneModal`.
 *
 * Quello che si vede quasi sempre è il campo chiuso, ed è identico a un `Input`. Cambia solo
 * l'elenco aperto, che lo disegna il sistema operativo: un compromesso voluto.
 */
function NativeSelect({ className, children, ...props }: React.ComponentProps<'select'>) {
  return (
    <div className="relative">
      <select
        data-slot="native-select"
        className={cn(
          // `dark:bg-input/30` non è un dettaglio: senza, in tema scuro il menu resta trasparente
          // mentre i campi di testo accanto hanno una leggera velatura, e nello stesso form si
          // vedono due tipi di campo diversi. Stessa classe di `Input` e del `Select` di Radix.
          'flex h-8 w-full appearance-none rounded-md border border-input bg-transparent dark:bg-input/30 px-2.5 py-1 pr-8 text-base transition-[color,box-shadow] outline-none',
          'focus-visible:border-ring focus-visible:ring-[3px] focus-visible:ring-ring/50',
          'disabled:cursor-not-allowed disabled:opacity-50',
          'aria-invalid:border-destructive aria-invalid:ring-destructive/20',
          'md:text-sm',
          className
        )}
        {...props}
      >
        {children}
      </select>
      {/* La freccia del browser sparisce con appearance-none: si ridisegna qui, così è uguale a
          quella del Select di Radix usato nei filtri. `pointer-events-none` serve perché il clic
          deve arrivare al campo sotto, non fermarsi sull'icona. */}
      <ChevronDownIcon className="pointer-events-none absolute right-2.5 top-1/2 size-4 -translate-y-1/2 opacity-50" />
    </div>
  )
}

export { NativeSelect }
