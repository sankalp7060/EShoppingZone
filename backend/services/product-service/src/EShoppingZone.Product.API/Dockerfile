# Use ASP.NET 8 Runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

# Use .NET 8 SDK for Build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy all projects for this service
COPY ["services/product-service/src/EShoppingZone.Product.API/EShoppingZone.Product.API.csproj", "services/product-service/src/EShoppingZone.Product.API/"]
COPY ["services/product-service/src/EShoppingZone.Product.Application/EShoppingZone.Product.Application.csproj", "services/product-service/src/EShoppingZone.Product.Application/"]
COPY ["services/product-service/src/EShoppingZone.Product.Domain/EShoppingZone.Product.Domain.csproj", "services/product-service/src/EShoppingZone.Product.Domain/"]
COPY ["services/product-service/src/EShoppingZone.Product.Infrastructure/EShoppingZone.Product.Infrastructure.csproj", "services/product-service/src/EShoppingZone.Product.Infrastructure/"]

# Restore
RUN dotnet restore "services/product-service/src/EShoppingZone.Product.API/EShoppingZone.Product.API.csproj"

# Copy everything
COPY . .
WORKDIR "/src/services/product-service/src/EShoppingZone.Product.API"
RUN dotnet build "EShoppingZone.Product.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "EShoppingZone.Product.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Final Image
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "EShoppingZone.Product.API.dll"]
