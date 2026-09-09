import * as React from 'react'
import { cva, type VariantProps } from 'class-variance-authority'
import { Slot } from 'radix-ui'

import { cn } from '@/lib/utils'

const buttonVariants = cva(
  "group/button inline-flex shrink-0 items-center justify-center rounded-lg border border-transparent bg-clip-padding text-sm font-medium whitespace-nowrap transition-all outline-none select-none focus-visible:border-ring focus-visible:ring-3 focus-visible:ring-ring/50 active:not-aria-[haspopup]:translate-y-px disabled:pointer-events-none disabled:opacity-50 aria-invalid:border-destructive aria-invalid:ring-3 aria-invalid:ring-destructive/20 dark:aria-invalid:border-destructive/50 dark:aria-invalid:ring-destructive/40 [&_svg]:pointer-events-none [&_svg]:shrink-0 [&_svg:not([class*='size-'])]:size-4",
  {
    variants: {
      variant: {
        default: 'bg-primary text-primary-foreground hover:bg-primary/80',
        outline:
          'border-border bg-background hover:bg-muted hover:text-foreground aria-expanded:bg-muted aria-expanded:text-foreground dark:border-input dark:bg-input/30 dark:hover:bg-input/50',
        secondary:
          'bg-secondary text-secondary-foreground hover:bg-[color-mix(in_oklch,var(--secondary),var(--foreground)_5%)] aria-expanded:bg-secondary aria-expanded:text-secondary-foreground',
        ghost:
          'hover:bg-muted hover:text-foreground aria-expanded:bg-muted aria-expanded:text-foreground dark:hover:bg-muted/50',
        destructive:
          'bg-destructive/10 text-destructive hover:bg-destructive/20 focus-visible:border-destructive/40 focus-visible:ring-destructive/20 dark:bg-destructive/20 dark:hover:bg-destructive/30 dark:focus-visible:ring-destructive/40',
        link: 'text-primary underline-offset-4 hover:underline',
        /*
         * Azione di riga (direzione «Turno»).
         *
         * Il pieno segnala l'azione primaria della PAGINA, e ce n'e' una sola: "Aggiungi
         * prenotazione". Venti pulsanti pieni incolonnati in una tabella non sono venti azioni
         * primarie, sono una campitura blu che ruba l'attenzione alle bande — dove la
         * saturazione ha un significato.
         *
         * A riposo la voce e' testo nel colore segnale a peso medio: pienamente leggibile
         * (7:1 sulla superficie della card, in entrambi i temi), non attenuata e non grigia.
         * Si riempie quando il puntatore entra nella RIGA (`group/riga`, non solo sul pulsante:
         * cosi' l'azione si accende mentre si legge la riga, non dopo averla centrata) e quando
         * arriva il fuoco da tastiera.
         *
         * Su touch l'hover non esiste: quello a riposo e' l'unico stato che l'utente vedra',
         * ed e' per questo che deve stare in piedi da solo. `pointer-coarse` porta l'area di
         * tocco a 44x44 senza allungare le righe su desktop.
         */
        azione: [
          // A riposo: contorno + colore segnale. Il contorno non e' un vezzo — senza, a riposo
          // questo e' testo colorato e basta, e non si capisce che si puo' premere finche' non
          // ci passi sopra. Su touch l'hover non arriva MAI, quindi a riposo e' l'unico stato
          // che quell'utente vedra': deve dire da solo "sono un comando".
          'bg-transparent text-primary font-medium border-azione-bordo',
          'hover:bg-primary hover:text-primary-foreground hover:border-primary',
          'focus-visible:bg-primary focus-visible:text-primary-foreground focus-visible:border-primary',
          'group-hover/riga:bg-primary group-hover/riga:text-primary-foreground group-hover/riga:border-primary',
          'group-focus-within/riga:bg-primary group-focus-within/riga:text-primary-foreground group-focus-within/riga:border-primary',
          'pointer-coarse:min-h-11 pointer-coarse:min-w-11',
        ].join(' '),
        /* Come sopra, ma per l'apri-menu: a riposo e' il colore del testo secondario, perche'
           non e' un'azione, e' un contenitore di azioni. */
        azioneMenu: [
          // L'apri-menu resta senza contorno: sta accanto all'azione con il contorno, e due
          // contorni affiancati li farebbero leggere come due comandi di pari peso — che e'
          // esattamente la fila di pulsanti uguali da cui si veniva. L'icona «…» e' gia' di per
          // se' un'affordance nota, e a riposo si vede perche' e' scura su fondo chiaro.
          'bg-transparent text-muted-foreground',
          'hover:bg-muted hover:text-foreground',
          'focus-visible:bg-muted focus-visible:text-foreground',
          'aria-expanded:bg-muted aria-expanded:text-foreground',
          'group-hover/riga:text-foreground',
          'pointer-coarse:min-h-11 pointer-coarse:min-w-11',
        ].join(' '),
      },
      size: {
        default:
          'h-8 gap-1.5 px-2.5 has-data-[icon=inline-end]:pr-2 has-data-[icon=inline-start]:pl-2',
        xs: "h-6 gap-1 rounded-[min(var(--radius-md),10px)] px-2 text-xs in-data-[slot=button-group]:rounded-lg has-data-[icon=inline-end]:pr-1.5 has-data-[icon=inline-start]:pl-1.5 [&_svg:not([class*='size-'])]:size-3",
        sm: "h-7 gap-1 rounded-[min(var(--radius-md),12px)] px-2.5 text-[0.8rem] in-data-[slot=button-group]:rounded-lg has-data-[icon=inline-end]:pr-1.5 has-data-[icon=inline-start]:pl-1.5 [&_svg:not([class*='size-'])]:size-3.5",
        lg: 'h-9 gap-1.5 px-2.5 has-data-[icon=inline-end]:pr-2 has-data-[icon=inline-start]:pl-2',
        icon: 'size-8',
        'icon-xs':
          "size-6 rounded-[min(var(--radius-md),10px)] in-data-[slot=button-group]:rounded-lg [&_svg:not([class*='size-'])]:size-3",
        'icon-sm':
          'size-7 rounded-[min(var(--radius-md),12px)] in-data-[slot=button-group]:rounded-lg',
        'icon-lg': 'size-9',
      },
    },
    defaultVariants: {
      variant: 'default',
      size: 'default',
    },
  }
)

function Button({
  className,
  variant = 'default',
  size = 'default',
  asChild = false,
  ...props
}: React.ComponentProps<'button'> &
  VariantProps<typeof buttonVariants> & {
    asChild?: boolean
  }) {
  const Comp = asChild ? Slot.Root : 'button'

  return (
    <Comp
      data-slot="button"
      data-variant={variant}
      data-size={size}
      className={cn(buttonVariants({ variant, size, className }))}
      {...props}
    />
  )
}

// eslint-disable-next-line react-refresh/only-export-components -- pattern standard shadcn/ui, non ristrutturare per restare compatibili con `npx shadcn add`
export { Button, buttonVariants }
