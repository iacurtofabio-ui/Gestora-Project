import { useZone } from '@/hooks/useZone'

export default function ZonePage() {
  const response = useZone()

  if (response.isLoading) return <div>Caricamento...</div>
  if (response.isError) return <div>Errore nel caricamento</div>

  return (
    <div className="bg-white rounded-lg border">
      <div className="flex justify-between items-center p-4 border-b">
        <h2 className="text-sm font-semibold text-gray-700">Zone</h2>
        <button className="bg-blue-500 text-white px-3 py-1 rounded text-sm">+ Aggiungi</button>
      </div>
      <table className="w-full text-sm">
        <thead>
          <tr className="border-b">
            <th className="text-left p-3">Nome</th>
            <th className="text-left p-3">Attiva</th>
            <th className="text-left p-3">Azioni</th>
          </tr>
        </thead>
        <tbody>
          {response.data?.map((zona) => (
            <tr key={zona.id} className="border-b">
              <td className="p-3">{zona.nome}</td>
              <td className="p-3">{zona.attiva ? 'Sì' : 'No'}</td>
              <td className="p-3 flex gap-2">
                <button className="text-blue-500 hover:underline text-sm">Modifica</button>
                <button className="text-red-500 hover:underline text-sm">Elimina</button>
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  )
}
