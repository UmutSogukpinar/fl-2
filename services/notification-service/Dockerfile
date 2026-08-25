# ===========================================
# ==========      Build Stage      ==========
# ===========================================

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

WORKDIR /App

# ============ Gather .project files, .slnx ============
# ==========     and restore dependencies     ==========

COPY FantasyLeague.NotificationService.slnx ./FantasyLeague.NotificationService.slnx

COPY src/FantasyLeague.Notification.Application/FantasyLeague.Notification.Application.csproj ./src/FantasyLeague.Notification.Application/FantasyLeague.Notification.Application.csproj
COPY src/FantasyLeague.Notification.Infrastructure/FantasyLeague.Notification.Infrastructure.csproj ./src/FantasyLeague.Notification.Infrastructure/FantasyLeague.Notification.Infrastructure.csproj
COPY src/FantasyLeague.Notification.Worker/FantasyLeague.Notification.Worker.csproj ./src/FantasyLeague.Notification.Worker/FantasyLeague.Notification.Worker.csproj

RUN dotnet restore

COPY . .

RUN dotnet build \
    src/FantasyLeague.Notification.Worker/FantasyLeague.Notification.Worker.csproj \
    --configuration Release \
    --no-restore


# =============================================
# ==========      Compile Stage      ==========
# =============================================

FROM build AS compile

RUN dotnet publish \
    src/FantasyLeague.Notification.Worker/FantasyLeague.Notification.Worker.csproj \
    --configuration Release \
    --no-build \
    --output /publish

# =============================================
# ===========      Final Stage      ===========
# =============================================

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final

WORKDIR /App

RUN apt-get update \
    && apt-get install -y --no-install-recommends libgssapi-krb5-2 \
    && rm -rf /var/lib/apt/lists/*

COPY --from=compile /publish ./

ENTRYPOINT ["dotnet", "FantasyLeague.Notification.Worker.dll"]
