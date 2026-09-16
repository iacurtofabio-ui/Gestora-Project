import { cn } from '@/lib/utils'

/**
 * Marchio di Gestora (Fase 13).
 *
 * Il simbolo è un tavolo visto dall'alto con quattro coperti: è il concetto su cui gira tutta
 * l'applicazione. È disegnato come SVG e non come immagine perché così prende i colori del tema
 * (`currentColor`) e resta nitido a qualsiasi dimensione, sul tema chiaro come su quello scuro.
 */
export function Logo({ className, conTesto = true }: { className?: string; conTesto?: boolean }) {
  return (
    <span className={cn('inline-flex items-center gap-2', className)}>
      <svg
        viewBox="0 0 32 32"
        className="size-7 shrink-0 text-primary"
        fill="none"
        stroke="currentColor"
        strokeWidth="2"
        strokeLinecap="round"
        aria-hidden="true"
      >
        {/* il tavolo */}
        <circle cx="16" cy="16" r="7" />
        {/* i quattro coperti attorno */}
        <path d="M16 3v3M16 26v3M3 16h3M26 16h3" />
      </svg>
      {conTesto && <span className="text-lg font-semibold tracking-tight">Gestora</span>}
    </span>
  )
}
