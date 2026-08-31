# Build ufficiale Microsoft, versione .NET pinnata esplicitamente (coerente con global.json) —
# niente auto-detection Nixpacks/nixpkgs, che per .NET 9 si è rivelata inaffidabile (SDK sbagliato
# la prima volta, SDK assente dal loro snapshot nixpkgs la seconda).
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copia solo i file di progetto per sfruttare la cache Docker sul restore: se cambia solo il
# codice sorgente, il layer di restore non viene rifatto.
COPY GestoraWebApi.csproj .
RUN dotnet restore GestoraWebApi.csproj

COPY . .
RUN dotnet publish GestoraWebApi.csproj -c Release -o /app/out --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
COPY --from=build /app/out .

# La porta effettiva arriva da ASPNETCORE_URLS (variabile d'ambiente Railway) — EXPOSE è solo
# documentazione, non vincola nulla a runtime.
EXPOSE 8080
ENTRYPOINT ["dotnet", "GestoraWebApi.dll"]
