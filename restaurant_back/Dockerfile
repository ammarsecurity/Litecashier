# Use the official .NET 8.0 SDK image for building
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy solution and project files
COPY RestaurantPOS.sln .
COPY RestaurantPOS/RestaurantPOS.csproj RestaurantPOS/
RUN dotnet restore RestaurantPOS/RestaurantPOS.csproj

# Copy all source files
COPY RestaurantPOS/ RestaurantPOS/

# Build the application
WORKDIR /src/RestaurantPOS
RUN dotnet build -c Release -o /app/build

# Publish the application
FROM build AS publish
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

# Use the official .NET 8.0 runtime image for running
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Install required packages for MySQL and health check
RUN apt-get update && \
    apt-get install -y --no-install-recommends \
    libc6 \
    libgcc1 \
    libgssapi-krb5-2 \
    libicu72 \
    libssl3 \
    libstdc++6 \
    zlib1g \
    wget && \
    rm -rf /var/lib/apt/lists/*

# Copy published application
COPY --from=publish /app/publish .

# Create directory for images
RUN mkdir -p /app/wwwroot/Images && chmod 755 /app/wwwroot/Images

# Set environment variables
ENV ASPNETCORE_URLS=http://+:5000
ENV ASPNETCORE_ENVIRONMENT=Production

# Health check (using wget which is available in the image)
HEALTHCHECK --interval=30s --timeout=10s --start-period=40s --retries=3 \
  CMD wget --no-verbose --tries=1 --spider http://localhost:5000/swagger/index.html || exit 1

# Run the application
ENTRYPOINT ["dotnet", "RestaurantPOS.dll"]

