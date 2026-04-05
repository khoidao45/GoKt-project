FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["src/Gokt.MatchingWorker/Gokt.MatchingWorker.csproj", "src/Gokt.MatchingWorker/"]
COPY ["src/Gokt.Application/Gokt.Application.csproj", "src/Gokt.Application/"]
COPY ["src/Gokt.Domain/Gokt.Domain.csproj", "src/Gokt.Domain/"]
COPY ["src/Gokt.Hubs/Gokt.Hubs.csproj", "src/Gokt.Hubs/"]
COPY ["src/Gokt.Infrastructure/Gokt.Infrastructure.csproj", "src/Gokt.Infrastructure/"]
RUN dotnet restore "src/Gokt.MatchingWorker/Gokt.MatchingWorker.csproj"
COPY . .
RUN dotnet publish "src/Gokt.MatchingWorker/Gokt.MatchingWorker.csproj" -c Release -o /app/publish --no-restore

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Gokt.MatchingWorker.dll"]
