# Use ASP.NET 8 Runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

# Use .NET 8 SDK for Build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy all projects first to cache restore
COPY ["shared/EShoppingZone.Common/EShoppingZone.Common.csproj", "shared/EShoppingZone.Common/"]
COPY ["api-gateway/EShoppingZone.Gateway/EShoppingZone.Gateway.csproj", "api-gateway/EShoppingZone.Gateway/"]

# Restore dependencies
RUN dotnet restore "api-gateway/EShoppingZone.Gateway/EShoppingZone.Gateway.csproj"

# Copy everything else and build
COPY . .
WORKDIR "/src/api-gateway/EShoppingZone.Gateway"
RUN dotnet build "EShoppingZone.Gateway.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "EShoppingZone.Gateway.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Final Image
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "EShoppingZone.Gateway.dll"]
