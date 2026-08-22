# Stage 1: Build & Publish
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy csproj files and restore dependencies
COPY ["src/InventoryHold.WebApi/InventoryHold.WebApi.csproj", "src/InventoryHold.WebApi/"]
COPY ["src/InventoryHold.Domain/InventoryHold.Domain.csproj", "src/InventoryHold.Domain/"]
COPY ["src/InventoryHold.Infrastructure/InventoryHold.Infrastructure.csproj", "src/InventoryHold.Infrastructure/"]
COPY ["src/InventoryHold.Contracts/InventoryHold.Contracts.csproj", "src/InventoryHold.Contracts/"]
RUN dotnet restore "src/InventoryHold.WebApi/InventoryHold.WebApi.csproj"

# Copy full source and build release
COPY . .
WORKDIR "/src/src/InventoryHold.WebApi"
RUN dotnet publish "InventoryHold.WebApi.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_HTTP_PORTS=8080

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "InventoryHold.WebApi.dll"]