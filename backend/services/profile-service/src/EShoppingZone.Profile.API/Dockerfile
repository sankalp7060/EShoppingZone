# Use ASP.NET 8 Runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

# Use .NET 8 SDK for Build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy all projects for this service
COPY ["services/profile-service/src/EShoppingZone.Profile.API/EShoppingZone.Profile.API.csproj", "services/profile-service/src/EShoppingZone.Profile.API/"]
COPY ["services/profile-service/src/EShoppingZone.Profile.Application/EShoppingZone.Profile.Application.csproj", "services/profile-service/src/EShoppingZone.Profile.Application/"]
COPY ["services/profile-service/src/EShoppingZone.Profile.Domain/EShoppingZone.Profile.Domain.csproj", "services/profile-service/src/EShoppingZone.Profile.Domain/"]
COPY ["services/profile-service/src/EShoppingZone.Profile.Infrastructure/EShoppingZone.Profile.Infrastructure.csproj", "services/profile-service/src/EShoppingZone.Profile.Infrastructure/"]

# Restore
RUN dotnet restore "services/profile-service/src/EShoppingZone.Profile.API/EShoppingZone.Profile.API.csproj"

# Copy everything
COPY . .
WORKDIR "/src/services/profile-service/src/EShoppingZone.Profile.API"
RUN dotnet build "EShoppingZone.Profile.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "EShoppingZone.Profile.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Final Image
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "EShoppingZone.Profile.API.dll"]
