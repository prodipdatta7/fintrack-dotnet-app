# Phase 2: Split Deployables Dockerfile (FinTrack.Api + FinTrack.Host)
# Multi-target build stage sharing SDK restoration and build output

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy solution and project definitions
COPY ["src/FinTrack.Api/FinTrack.Api.csproj", "src/FinTrack.Api/"]
COPY ["src/FinTrack.Host/FinTrack.Host.csproj", "src/FinTrack.Host/"]
COPY ["src/FinTrack.BuildingBlocks/FinTrack.BuildingBlocks.csproj", "src/FinTrack.BuildingBlocks/"]
COPY ["src/FinTrack.Contracts/FinTrack.Contracts.csproj", "src/FinTrack.Contracts/"]
COPY ["src/Modules/Dashboard/FinTrack.Modules.Dashboard.csproj", "src/Modules/Dashboard/"]
COPY ["src/Modules/Transactions/FinTrack.Modules.Transactions.csproj", "src/Modules/Transactions/"]
COPY ["src/Modules/Categories/FinTrack.Modules.Categories.csproj", "src/Modules/Categories/"]
COPY ["src/Modules/Budgets/FinTrack.Modules.Budgets.csproj", "src/Modules/Budgets/"]
COPY ["src/Modules/Accounts/FinTrack.Modules.Accounts.csproj", "src/Modules/Accounts/"]
COPY ["src/Modules/Users/FinTrack.Modules.Users.csproj", "src/Modules/Users/"]

RUN dotnet restore "src/FinTrack.Api/FinTrack.Api.csproj"
RUN dotnet restore "src/FinTrack.Host/FinTrack.Host.csproj"

COPY . .

# Build API Target
FROM build AS publish-api
RUN dotnet publish "src/FinTrack.Api/FinTrack.Api.csproj" -c Release -o /app/api /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS api
WORKDIR /app
COPY --from=publish-api /app/api .
EXPOSE 8080
ENTRYPOINT ["dotnet", "FinTrack.Api.dll"]

# Build Worker Host Target
FROM build AS publish-host
RUN dotnet publish "src/FinTrack.Host/FinTrack.Host.csproj" -c Release -o /app/host /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS host
WORKDIR /app
COPY --from=publish-host /app/host .
ENTRYPOINT ["dotnet", "FinTrack.Host.dll"]
