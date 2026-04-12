# Use ASP.NET 8 Runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

# Use .NET 8 SDK for Build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Context is set to 'backend' folder
COPY . .

# Restore and Build
RUN dotnet restore "services/wallet-service/src/EShoppingZone.Wallet.API/EShoppingZone.Wallet.API.csproj"
WORKDIR "/src/services/wallet-service/src/EShoppingZone.Wallet.API"
RUN dotnet build "EShoppingZone.Wallet.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "EShoppingZone.Wallet.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Final Image
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "EShoppingZone.Wallet.API.dll"]
