# Phase 1: Single-Process Deployment Dockerfile (FinTrack.Api)
# Multi-stage build for .NET 10 ASP.NET Core Web API + in-process modular monolith

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER app
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Copy project files for dependency caching
COPY ["src/FinTrack.Api/FinTrack.Api.csproj", "src/FinTrack.Api/"]
COPY ["src/FinTrack.BuildingBlocks/FinTrack.BuildingBlocks.csproj", "src/FinTrack.BuildingBlocks/"]
COPY ["src/FinTrack.Contracts/FinTrack.Contracts.csproj", "src/FinTrack.Contracts/"]
COPY ["src/FinTrack.Modules.Dashboard/FinTrack.Modules.Dashboard.csproj", "src/FinTrack.Modules.Dashboard/"]
COPY ["src/FinTrack.Modules.Transactions/FinTrack.Modules.Transactions.csproj", "src/FinTrack.Modules.Transactions/"]
COPY ["src/FinTrack.Modules.Categories/FinTrack.Modules.Categories.csproj", "src/FinTrack.Modules.Categories/"]
COPY ["src/FinTrack.Modules.Budgets/FinTrack.Modules.Budgets.csproj", "src/FinTrack.Modules.Budgets/"]
COPY ["src/FinTrack.Modules.Accounts/FinTrack.Modules.Accounts.csproj", "src/FinTrack.Modules.Accounts/"]
COPY ["src/FinTrack.Modules.Users/FinTrack.Modules.Users.csproj", "src/FinTrack.Modules.Users/"]

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
