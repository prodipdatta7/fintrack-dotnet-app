# Build from repo root: fintrack-dotnet-app
#   docker build -t fintrack-api -f src/FinTrack.Api/Dockerfile .
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY src/FinTrack.BuildingBlocks/FinTrack.BuildingBlocks.csproj FinTrack.BuildingBlocks/
COPY src/FinTrack.Contracts/FinTrack.Contracts.csproj FinTrack.Contracts/
COPY src/FinTrack.Modules.Users/FinTrack.Modules.Users.csproj FinTrack.Modules.Users/
COPY src/FinTrack.Modules.Transactions/FinTrack.Modules.Transactions.csproj FinTrack.Modules.Transactions/
COPY src/FinTrack.Modules.Categories/FinTrack.Modules.Categories.csproj FinTrack.Modules.Categories/
COPY src/FinTrack.Modules.Dashboard/FinTrack.Modules.Dashboard.csproj FinTrack.Modules.Dashboard/
COPY src/FinTrack.Modules.Budgets/FinTrack.Modules.Budgets.csproj FinTrack.Modules.Budgets/
COPY src/FinTrack.Modules.Accounts/FinTrack.Modules.Accounts.csproj FinTrack.Modules.Accounts/
COPY src/FinTrack.Api/FinTrack.Api.csproj FinTrack.Api/

RUN dotnet restore FinTrack.Api/FinTrack.Api.csproj

COPY src/FinTrack.BuildingBlocks/ FinTrack.BuildingBlocks/
COPY src/FinTrack.Contracts/ FinTrack.Contracts/
COPY src/FinTrack.Modules.Users/ FinTrack.Modules.Users/
COPY src/FinTrack.Modules.Transactions/ FinTrack.Modules.Transactions/
COPY src/FinTrack.Modules.Categories/ FinTrack.Modules.Categories/
COPY src/FinTrack.Modules.Dashboard/ FinTrack.Modules.Dashboard/
COPY src/FinTrack.Modules.Budgets/ FinTrack.Modules.Budgets/
COPY src/FinTrack.Modules.Accounts/ FinTrack.Modules.Accounts/
COPY src/FinTrack.Api/ FinTrack.Api/

RUN dotnet publish FinTrack.Api/FinTrack.Api.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production
EXPOSE 8080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "FinTrack.Api.dll"]
