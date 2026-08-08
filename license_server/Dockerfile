FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY LicenseServer.csproj .
RUN dotnet restore
COPY . .
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
RUN mkdir -p /app/data && chmod 755 /app/data
ENV ASPNETCORE_URLS=http://+:5099
ENV ASPNETCORE_ENVIRONMENT=Production
EXPOSE 5099
ENTRYPOINT ["dotnet", "LicenseServer.dll"]
