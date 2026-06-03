# Guida CRUD Frontend — Gestora

Ogni modulo CRUD (Zone, Postazioni, Fasce Orarie...) segue sempre la stessa sequenza di 4 file.
Leggi questa guida dall'inizio ogni volta che attacchi un modulo nuovo.

---

## La sequenza obbligatoria

```
1. src/types/nomeEntità.ts            ← definisce la forma dei dati
2. src/hooks/useNomeEntità.ts         ← comunica con il backend
3. src/components/NomeEntitàModal.tsx ← form per creare e modificare
4. src/pages/NomeEntitàPage.tsx       ← mostra i dati e importa il modal
```

> **Perché il modal prima della page?**
> La page importa il modal — se il modal non esiste, TypeScript dà errore.
> Si costruisce prima il pezzo più piccolo (modal) e poi quello che lo usa (page).

---

## STEP 1 — Types (`src/types/nomeEntità.ts`)

### Cosa sono i tipi?

I tipi TypeScript descrivono la **forma** di un oggetto: quali campi ha e di che tipo sono.
Servono a TypeScript per avvisarti subito se stai passando un dato sbagliato.

### Quanti tipi creare?

Sempre **due**:

- **`NomeEntitàDTO`** — specchio esatto del DTO C# del backend. Include `id`.
  Usato quando *ricevi* dati dal backend (lista, dettaglio).

- **`NomeEntitàFormDTO`** — come il DTO ma **senza `id`**.
  Usato quando *mandi* dati al backend per creare o aggiornare.
  Per l'update aggiungi `id` con lo spread operator nel momento in cui chiami la mutazione.

### Regole di traduzione C# → TypeScript

| C#           | TypeScript  |
|--------------|-------------|
| `long`       | `number`    |
| `int`        | `number`    |
| `string`     | `string`    |
| `bool`       | `boolean`   |
| `DayOfWeek`  | `number`    |
| `List<T>`    | `T[]`       |

I nomi dei campi vanno in **camelCase**: `OrarioInizio` → `orarioInizio`.

### Esempio (Postazione)

```ts
// src/types/postazione.ts

export type PostazioneDTO = {
    id: number
    numero: number
    capienzaMassima: number
    zonaId: number
    attiva: boolean
    prenotazioneId: number[]
}

export type PostazioneFormDTO = {
    numero: number
    capienzaMassima: number
    zonaId: number
    attiva: boolean
}
```

---

## STEP 2 — Hooks (`src/hooks/useNomeEntità.ts`)

### Cosa sono gli hook?

Gli hook sono funzioni speciali di React (iniziano sempre con `use`).
In questo progetto gli hook fanno una cosa sola: **parlare con il backend**.

Usiamo due tipi di hook da React Query:

- **`useQuery`** — per le **letture** (GET). Carica i dati automaticamente quando il componente si monta.
- **`useMutation`** — per le **scritture** (POST, PUT, DELETE). Si attiva solo quando chiami `.mutate()`.

### Struttura hook di lettura (useQuery)

```ts
export function useNomeEntità() {
    return useQuery<NomeEntitàDTO[]>({
        queryKey: ['nomeEntità'],       // chiave univoca per la cache
        queryFn: () => apiClient.get('/Endpoint/get-all').then(r => r.data),
    })
}
```

- `queryKey` — React Query usa questa chiave per cacheare e invalidare i dati.
  Deve essere uguale ovunque si parla della stessa entità.
- `queryFn` — la funzione che chiama l'API. `r.data` è il corpo della risposta.

### Struttura hook di scrittura (useMutation)

```ts
export function useCreaQualcosa() {
    const queryClient = useQueryClient()
    return useMutation({
        mutationFn: (data: NomeEntitàFormDTO) => apiClient.post('/Endpoint/crea', data),
        onSuccess: () => {
            queryClient.invalidateQueries({ queryKey: ['nomeEntità'] })  // ricarica la lista
            toast.success('Elemento creato con successo')
        },
        onError: (error: any) => {
            const msg = error?.response?.data?.message ?? 'Errore imprevisto'
            toast.error(msg)
        }
    })
}
```

- `invalidateQueries` — dice a React Query "i dati in cache non sono più validi, ricaricali".
  Usa la stessa `queryKey` del `useQuery` corrispondente.
- `toast` — notifica visiva. Va messo nell'hook, non nel componente (evita doppi toast).

### Regola importante su update vs create

- **Create** (`useCreaX`) → accetta `FormDTO` (senza id)
- **Update** (`useUpdateX`) → accetta `DTO` completo (con id)
- **Delete** (`useDeleteX`) → accetta solo `id: number`

### Esempio completo (hook delete)

```ts
export function useDeletePostazione() {
    const queryClient = useQueryClient()
    return useMutation({
        mutationFn: (id: number) => apiClient.delete(`/Postazione/delete-postazione?id=${id}`),
        onSuccess: () => {
            queryClient.invalidateQueries({ queryKey: ['postazioni'] })
            toast.success('Postazione eliminata con successo')
        },
        onError: (error: any) => {
            const msg = error?.response?.data?.message ?? 'Errore durante l\'eliminazione'
            toast.error(msg)
        }
    })
}
```

> **Nota endpoint**: alcuni endpoint usano path param (`/delete/{id}`), altri query param (`?id={id}`).
> Guarda sempre il controller C# per capire quale usa — se non c'è `[FromRoute]` è query param.

---

## STEP 3 — Page (`src/pages/NomeEntitàPage.tsx`)

### Cosa fa la pagina?

La pagina è il componente principale del modulo. Si occupa di:
1. Caricare i dati (usando gli hook)
2. Mostrarli in una tabella
3. Aprire il modal per creare/modificare
4. Gestire il delete direttamente

### Struttura tipo

```tsx
import { useState } from 'react'
import type { NomeEntitàDTO } from '@/types/nomeEntità'
import { useNomeEntità, useDeleteNomeEntità } from '@/hooks/useNomeEntità'
import NomeEntitàModal from '@/components/NomeEntitàModal'

export default function NomeEntitàPage() {
    // --- stato UI ---
    const [isModalOpen, setIsModalOpen] = useState(false)
    const [elementoSelezionato, setElementoSelezionato] = useState<NomeEntitàDTO | undefined>(undefined)

    // --- hook dati ---
    const { data, isLoading, isError } = useNomeEntità()
    const deleteElemento = useDeleteNomeEntità()

    // --- stati di caricamento ---
    if (isLoading) return <div>Caricamento...</div>
    if (isError) return <div>Errore nel caricamento</div>

    return (
        <div className="bg-white rounded-lg border">
            {/* HEADER */}
            <div className="flex justify-between items-center p-4 border-b">
                <h2 className="text-sm font-semibold text-gray-700">Nome Entità</h2>
                <button
                    className="bg-blue-500 text-white px-3 py-1 rounded text-sm"
                    onClick={() => { setElementoSelezionato(undefined); setIsModalOpen(true) }}
                >
                    + Aggiungi
                </button>
            </div>

            {/* TABELLA */}
            <table className="w-full text-sm">
                <thead>
                    <tr className="border-b">
                        <th className="text-left p-3">Campo 1</th>
                        <th className="text-left p-3">Campo 2</th>
                        <th className="text-left p-3">Azioni</th>
                    </tr>
                </thead>
                <tbody>
                    {data?.map((elemento) => (
                        <tr key={elemento.id} className="border-b">
                            <td className="p-3">{elemento.campo1}</td>
                            <td className="p-3">{elemento.campo2}</td>
                            <td className="p-3 flex gap-2">
                                <button
                                    className="bg-blue-500 text-white px-3 py-1 rounded text-sm"
                                    onClick={() => { setElementoSelezionato(elemento); setIsModalOpen(true) }}
                                >
                                    Modifica
                                </button>
                                <button
                                    className="text-red-500 hover:underline text-sm"
                                    onClick={() => deleteElemento.mutate(elemento.id)}
                                >
                                    Elimina
                                </button>
                            </td>
                        </tr>
                    ))}
                </tbody>
            </table>

            {/* MODAL */}
            <NomeEntitàModal
                isOpen={isModalOpen}
                onClose={() => setIsModalOpen(false)}
                elemento={elementoSelezionato}
            />
        </div>
    )
}
```

### Logica Modifica vs Aggiungi

Il modal è uno solo — funziona sia per creare che per modificare.
Il trucco è lo stato `elementoSelezionato`:

- Clicco **Aggiungi** → `setElementoSelezionato(undefined)` → modal riceve `undefined` → sa che deve creare
- Clicco **Modifica** → `setElementoSelezionato(elemento)` → modal riceve il dato → sa che deve aggiornare

---

## STEP 4 — Modal (`src/components/NomeEntitàModal.tsx`)

### Cosa fa il modal?

Il modal contiene il form per creare e modificare. Usa due librerie:

- **React Hook Form** — gestisce lo stato del form (valori, errori, submit)
- **Zod** — valida i dati prima di inviarli al backend

### Il flusso

```
Utente compila form
    → React Hook Form raccoglie i valori
        → Zod li valida
            → se ok → onSubmit viene chiamato con i dati validati
                → si chiama la mutazione
                    → onSuccess: toast + onClose()
                    → onError: toast con messaggio backend
```

### Struttura tipo

```tsx
import { Dialog, DialogContent, DialogHeader, DialogTitle } from "@/components/ui/dialog"
import { useForm } from 'react-hook-form'
import { z } from 'zod'
import { zodResolver } from '@hookform/resolvers/zod'
import { useEffect } from 'react'
import { toast } from 'sonner'
import type { NomeEntitàDTO } from "@/types/nomeEntità"
import { useCreaX, useUpdateX } from "@/hooks/useNomeEntità"

// 1. Schema Zod — definisce le regole di validazione
const schema = z.object({
    campoStringa: z.string().min(1, 'Campo obbligatorio'),
    campoNumero: z.number().min(1, 'Campo obbligatorio'),
    campoBoolean: z.boolean(),
})

type NomeEntitàForm = z.infer<typeof schema>  // ricava il tipo TypeScript dallo schema

type Props = {
    isOpen: boolean
    onClose: () => void
    elemento?: NomeEntitàDTO  // undefined = crea, valorizzato = modifica
}

export default function NomeEntitàModal({ isOpen, onClose, elemento }: Props) {
    const creaX = useCreaX()
    const updateX = useUpdateX()

    // 2. Setup form con valori default
    const { register, handleSubmit, formState: { errors, isSubmitting }, reset } = useForm<NomeEntitàForm>({
        resolver: zodResolver(schema),
        defaultValues: {
            campoStringa: elemento?.campoStringa ?? '',
            campoNumero: elemento?.campoNumero,   // undefined (non 0) per i numeri con min(1)
            campoBoolean: elemento?.campoBoolean ?? true,
        },
    })

    // 3. Reset form quando cambia l'elemento (es. apro modifica dopo aver aperto crea)
    useEffect(() => {
        reset({
            campoStringa: elemento?.campoStringa ?? '',
            campoNumero: elemento?.campoNumero,
            campoBoolean: elemento?.campoBoolean ?? true,
        })
    }, [elemento])

    // 4. Submit — distingue crea da update
    function onSubmit(data: NomeEntitàForm) {
        if (elemento) {
            // UPDATE: aggiungo id con spread operator
            updateX.mutate({ ...data, id: elemento.id }, {
                onSuccess: () => onClose(),
                onError: (error: any) => {
                    const errs = error?.response?.data?.errors as { field: string; error: string }[] | undefined
                    if (errs && errs.length > 0) {
                        errs.forEach(e => toast.error(e.error))
                    } else {
                        toast.error(error?.response?.data?.message ?? 'Errore imprevisto')
                    }
                }
            })
        } else {
            // CREATE
            creaX.mutate(data, {
                onSuccess: () => onClose(),
                onError: (error: any) => {
                    const errs = error?.response?.data?.errors as { field: string; error: string }[] | undefined
                    if (errs && errs.length > 0) {
                        errs.forEach(e => toast.error(e.error))
                    } else {
                        toast.error(error?.response?.data?.message ?? 'Errore imprevisto')
                    }
                }
            })
        }
    }

    return (
        <Dialog open={isOpen} onOpenChange={onClose}>
            <DialogContent>
                <DialogHeader>
                    <DialogTitle>{elemento ? 'Modifica' : 'Nuovo'}</DialogTitle>
                </DialogHeader>
                <form onSubmit={handleSubmit(onSubmit)} className="flex flex-col gap-4">

                    {/* Input testo */}
                    <div>
                        <label className="text-sm font-medium text-gray-700">Campo stringa</label>
                        <input
                            {...register('campoStringa')}
                            type="text"
                            className="w-full border rounded px-3 py-2"
                        />
                        {errors.campoStringa && <p className="text-red-500 text-sm mt-1">{errors.campoStringa.message}</p>}
                    </div>

                    {/* Input numero — SEMPRE valueAsNumber: true */}
                    <div>
                        <label className="text-sm font-medium text-gray-700">Campo numero</label>
                        <input
                            {...register('campoNumero', { valueAsNumber: true })}
                            type="number"
                            className="w-full border rounded px-3 py-2"
                        />
                        {errors.campoNumero && <p className="text-red-500 text-sm mt-1">{errors.campoNumero.message}</p>}
                    </div>

                    {/* Select (es. zona, giorno settimana) — SEMPRE valueAsNumber: true se il valore è un numero */}
                    <div>
                        <label className="text-sm font-medium text-gray-700">Campo select</label>
                        <select
                            {...register('campoSelect', { valueAsNumber: true })}
                            className="w-full border rounded px-3 py-2"
                        >
                            <option value="">-- Seleziona --</option>
                            {/* opzioni dinamiche o statiche */}
                        </select>
                        {errors.campoSelect && <p className="text-red-500 text-sm mt-1">{errors.campoSelect.message}</p>}
                    </div>

                    {/* Checkbox */}
                    <div className="flex items-center gap-2">
                        <input {...register('campoBoolean')} type="checkbox" className="h-4 w-4" />
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
```

### Regole d'oro del modal

| Situazione | Cosa fare |
|---|---|
| Campo numerico | Usare `type="number"` + `valueAsNumber: true` nel register |
| Campo select con valore numerico | Usare `valueAsNumber: true` nel register |
| Zod schema con `z.number().min(1)` | Default value `undefined`, mai `0` |
| Zod schema con `z.string().min(1)` | Default value `''` |
| Zod schema con `z.boolean()` | Default value `true` o `false` |
| Toast | Metterlo nell'hook (`onSuccess`/`onError` del `useMutation`), non nel componente |
| Chiusura modal | `onClose()` solo nell'`onSuccess` del `mutate()`, mai fuori |

---

## Checklist — da spuntare per ogni modulo

- [ ] `types/nomeEntità.ts` — DTO e FormDTO con tipi corretti
- [ ] `hooks/useNomeEntità.ts` — useQuery + useMutation (crea, update, delete)
- [ ] `pages/NomeEntitàPage.tsx` — tabella + stati UI + delete
- [ ] `components/NomeEntitàModal.tsx` — form con Zod + reset + submit
- [ ] Route aggiunta in `src/router/index.tsx`
- [ ] Link aggiunto nella sidebar

---

## Errori comuni da evitare

1. **Hooks fuori dal componente** — tutti gli `useState`, `useQuery` ecc. devono stare DENTRO la funzione componente
2. **Variable shadowing** — non usare lo stesso nome per la variabile di stato e il parametro del `.map()`
3. **`z.number()` con default `0`** — se lo schema ha `.min(1)`, il default deve essere `undefined` non `0`
4. **`valueAsNumber` mancante** — ogni `input type="number"` e `select` con valore numerico deve averlo
5. **`onClose()` fuori da `onSuccess`** — il modal si chiuderebbe anche in caso di errore
6. **Toast doppio** — il toast va solo nell'hook, non anche nel componente
7. **QueryKey diversa** — `invalidateQueries` deve usare esattamente la stessa stringa del `queryKey` in `useQuery`
