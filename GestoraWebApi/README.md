# Gestora — backend

API per Gestora, gestionale di prenotazioni per attività con posti a sedere (ristoranti, pub,
pizzerie): configurazione della sala, prenotazioni, assegnazione automatica dei tavoli.

ASP.NET Core 9 · Entity Framework Core 9 + PostgreSQL · ASP.NET Identity + JWT · Quartz.NET ·
FluentValidation · AutoMapper · Serilog.

## Avvio in locale

```powershell
dotnet run
```

Richiede PostgreSQL attivo e i due User Secrets (`ConnectionStrings:DefaultConnection`,
`JwtSettings:Secret`) — vedi `docs/progetto/RUNBOOK.md` §7. Ascolta su `localhost:5099`.

## Verifica

```powershell
dotnet test
```

## Dove trovare il resto

Questo repository fa parte di Gestora insieme a `gestora-frontend`. La documentazione trasversale
al progetto (stato, decisioni di prodotto, procedure operative, storico) vive in
`docs/progetto/` in questo stesso repository — è la copia di riferimento dopo la separazione dei
due repository (vedi `docs/progetto/RUNBOOK.md` §10).

Documentazione specifica di questo backend: `CLAUDE.md`.
