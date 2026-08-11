# ===========================================
# ==========      Build Stage      ==========
# ===========================================

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

WORKDIR /App

# ============ Gather .project files, .slnx ============
# ==========     and restore dependencies     ==========

COPY fantasy_league.slnx ./fantasy_league.slnx

COPY src/FantasyLeague.Application/FantasyLeague.Application.csproj ./src/FantasyLeague.Application/FantasyLeague.Application.csproj
COPY src/FantasyLeague.Domain/FantasyLeague.Domain.csproj ./src/FantasyLeague.Domain/FantasyLeague.Domain.csproj
COPY src/FantasyLeague.Infrastructure/FantasyLeague.Infrastructure.csproj ./src/FantasyLeague.Infrastructure/FantasyLeague.Infrastructure.csproj
COPY src/FantasyLeague.WebApi/FantasyLeague.WebApi.csproj ./src/FantasyLeague.WebApi/FantasyLeague.WebApi.csproj

COPY tests/FantasyLeague.Application.Tests/FantasyLeague.Application.Tests.csproj ./tests/FantasyLeague.Application.Tests/FantasyLeague.Application.Tests.csproj
COPY tests/FantasyLeague.Infrastructure.IntegrationTests/FantasyLeague.Infrastructure.IntegrationTests.csproj ./tests/FantasyLeague.Infrastructure.IntegrationTests/FantasyLeague.Infrastructure.IntegrationTests.csproj
COPY tests/FantasyLeague.WebApi.IntegrationTests/FantasyLeague.WebApi.IntegrationTests.csproj ./tests/FantasyLeague.WebApi.IntegrationTests/FantasyLeague.WebApi.IntegrationTests.csproj

RUN dotnet restore

COPY . .

RUN dotnet build \
    src/FantasyLeague.WebApi/FantasyLeague.WebApi.csproj \
    --configuration Release \
    --no-restore

# ============================================
# ===========      Test Stage      ===========
# ============================================

FROM build AS test

## ===== Execute tests and generate test results in trx format =====

RUN dotnet test fantasy_league.slnx \
    --configuration Release \
    --no-build \
    --logger "trx" \
    --results-directory /test-results


# =============================================
# ==========      Compile Stage      ==========
# =============================================

FROM build AS compile

RUN dotnet publish \
    src/FantasyLeague.WebApi/FantasyLeague.WebApi.csproj \
    --configuration Release \
    --no-restore \
    --output /publish

# =============================================
# ===========      Final Stage      ===========
# =============================================

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final

WORKDIR /App

COPY --from=compile /publish ./

ENTRYPOINT ["dotnet", "FantasyLeague.WebApi.dll"]
