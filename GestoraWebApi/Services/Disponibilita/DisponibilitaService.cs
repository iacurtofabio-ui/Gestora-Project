using GestoraWebApi.Models;
using GestoraWebApi.Repositories.Prenotazioni;
using GestoraWebApi.Services.PostazioneAssignment;
using GestoraWebApi.Services.PrenotazioniPostazioni;

namespace GestoraWebApi.Services.Disponibilita
{
    public class DisponibilitaService : IDisponibilitaService
    {
        private readonly IPrenotazioniRepository _prenotazioniRepository;
        private readonly IPostazioneAssignmentService _postazioneAssignmentService;

        public DisponibilitaService(IPrenotazioniRepository prenotazioniRepository,
                                    IPostazioneAssignmentService postazioneAssignmentService)
        {
            _prenotazioniRepository = prenotazioniRepository;
            _postazioneAssignmentService = postazioneAssignmentService;
        }

        public async Task<DisponibilitaResponseDTO> CheckDisponibilitaAsync(CheckDisponibilitaDTO dto)
        {
            var dayOfWeek = dto.DataPrenotazione.DayOfWeek;

            var fasce = await _prenotazioniRepository.GetFasceOrarieByDayAsync(dayOfWeek);
            var postazioni = await _prenotazioniRepository.GetAllPostazioniAsync();
            var prenotazioni = await _prenotazioniRepository.GetPrenotazioniByDataAsync(dto.DataPrenotazione);

            var assegnate = prenotazioni
                .SelectMany(p => p.PrenotazioniPostazioni ?? Enumerable.Empty<PrenotazionePostazione>(),
                            (p, pp) => new { p.FasciaOrariaId, PostazioneId = pp.PostazioneId, pp.NumeroPosti })
                .ToList();

            var groupedAssegnate = assegnate
                .GroupBy(x => new { x.FasciaOrariaId, x.PostazioneId })
                .ToDictionary(g => (g.Key.FasciaOrariaId, g.Key.PostazioneId), g => g.Sum(x => x.NumeroPosti));

            var nonAssegnatePerFascia = prenotazioni
                .Where(p => p.PrenotazioniPostazioni == null || !p.PrenotazioniPostazioni.Any())
                .GroupBy(p => p.FasciaOrariaId)
                .ToDictionary(g => g.Key, g => g.Sum(p => p.NumeroCoperti));

            var totalCapacityAllPostazioni = postazioni.Sum(p => p.CapienzaMassima);
            var response = new DisponibilitaResponseDTO();

            foreach (var f in fasce)
            {
                var prenotazioniFascia = prenotazioni.Where(p => p.FasciaOrariaId == f.Id).ToList();

                if (f.MaxPrenotazioni > 0 && prenotazioniFascia.Count >= f.MaxPrenotazioni)
                    continue;

                var fasciaDto = new FasciaDisponibilitaDTO
                {
                    FasciaOrariaId = f.Id,
                    OrarioInizio = f.OrarioInizio,
                    OrarioFine = f.OrarioFine
                };

                var occupiedPerFascia = groupedAssegnate
                    .Where(kvp => kvp.Key.Item1 == f.Id)
                    .GroupBy(kvp => kvp.Key.Item2)
                    .ToDictionary(g => g.Key, g => g.Sum(kvp => kvp.Value));

                int allocatedSumForFascia = groupedAssegnate
                    .Where(kvp => kvp.Key.Item1 == f.Id)
                    .Sum(kvp => kvp.Value);

                int unassigned = nonAssegnatePerFascia.TryGetValue(f.Id, out var u) ? u : 0;
                int totalReserved = allocatedSumForFascia + unassigned;
                int totalAvailable = totalCapacityAllPostazioni - totalReserved;

                fasciaDto.TotaleCapienza = totalCapacityAllPostazioni;
                fasciaDto.TotalePostiDisponibili = Math.Max(0, totalAvailable);
                fasciaDto.DisponibilePerRichiesta = fasciaDto.TotalePostiDisponibili >= dto.NumeroCoperti;

                var postazioniLibere = postazioni
                    .Where(p => !occupiedPerFascia.ContainsKey(p.Id))
                    .ToList();

                var combinazioni = _postazioneAssignmentService.TrovaCombinazioniDisponibili(postazioniLibere, dto.NumeroCoperti);

                foreach (var combo in combinazioni)
                {
                    fasciaDto.Postazioni.AddRange(combo.Select(p => new PostazioneDisponibilitaDTO
                    {
                        PostazioneId = p.Id,
                        Numero = p.Numero,
                        Capienza = p.CapienzaMassima,
                        PostiOccupati = occupiedPerFascia.TryGetValue(p.Id, out var occ) ? occ : 0,
                        PostiDisponibili = Math.Max(0, p.CapienzaMassima - (occupiedPerFascia.TryGetValue(p.Id, out var occ2) ? occ2 : 0)),
                        DisponibilePerRichiesta = true
                    }));
                }

                if (fasciaDto.Postazioni.Any())
                    response.Fasce.Add(fasciaDto);
            }

            return response;
        }
    }
}
