# Use ASP.NET 8 Runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

# Use .NET 8 SDK for Build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy EVERYTHING to maintain relative paths
COPY . .

# Restore and Build
RUN dotnet restore "backend/services/product-service/src/EShoppingZone.Product.API/EShoppingZone.Product.API.csproj"
WORKDIR "/src/backend/services/product-service/src/EShoppingZone.Product.API"
RUN dotnet build "EShoppingZone.Product.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "EShoppingZone.Product.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Final Image
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "EShoppingZone.Product.API.dll"]
