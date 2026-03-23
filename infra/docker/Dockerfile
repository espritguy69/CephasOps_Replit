# Build from repo root: docker build -f infra/docker/Dockerfile .
# Multi-stage: build then runtime
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY backend/ ./backend/
COPY analyzers/ ./analyzers/

WORKDIR /src/backend/src/CephasOps.Api
RUN dotnet restore
RUN dotnet publish -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENTRYPOINT ["dotnet", "CephasOps.Api.dll"]
