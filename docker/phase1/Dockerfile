# Phase 1: Single-Process Deployment Dockerfile (FinTrack.Api)
# Multi-stage build for .NET 9 ASP.NET Core Web API + in-process modular monolith

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
USER app
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Copy project files for dependency caching
COPY ["src/FinTrack.Api/FinTrack.Api.csproj", "src/FinTrack.Api/"]
COPY ["src/FinTrack.BuildingBlocks/FinTrack.BuildingBlocks.csproj", "src/FinTrack.BuildingBlocks/"]
COPY ["src/FinTrack.Contracts/FinTrack.Contracts.csproj", "src/FinTrack.Contracts/"]
COPY ["src/Modules/Dashboard/FinTrack.Modules.Dashboard.csproj", "src/Modules/Dashboard/"]
COPY ["src/Modules/Transactions/FinTrack.Modules.Transactions.csproj", "src/Modules/Transactions/"]
COPY ["src/Modules/Categories/FinTrack.Modules.Categories.csproj", "src/Modules/Categories/"]
COPY ["src/Modules/Budgets/FinTrack.Modules.Budgets.csproj", "src/Modules/Budgets/"]
COPY ["src/Modules/Accounts/FinTrack.Modules.Accounts.csproj", "src/Modules/Accounts/"]
COPY ["src/Modules/Users/FinTrack.Modules.Users.csproj", "src/Modules/Users/"]

RUN dotnet restore "src/FinTrack.Api/FinTrack.Api.csproj"

# Copy remaining source code and compile
COPY . .
WORKDIR "/src/src/FinTrack.Api"
RUN dotnet build "FinTrack.Api.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "FinTrack.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "FinTrack.Api.dll"]
