FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY POS/POS.csproj POS/
RUN dotnet restore POS/POS.csproj
COPY POS/ POS/
WORKDIR /src/POS
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
RUN apt-get update && \
    apt-get install -y --no-install-recommends wget && \
    rm -rf /var/lib/apt/lists/*
COPY --from=build /app/publish .
RUN mkdir -p /app/wwwroot/Images /app/wwwroot/app && chmod -R 755 /app/wwwroot
ENV ASPNETCORE_URLS=http://+:6768
ENV ASPNETCORE_ENVIRONMENT=Production
EXPOSE 6768
HEALTHCHECK --interval=30s --timeout=10s --start-period=40s --retries=3 \
  CMD wget --no-verbose --tries=1 --spider http://localhost:6768/swagger/index.html || exit 1
ENTRYPOINT ["dotnet", "POS.dll"]
