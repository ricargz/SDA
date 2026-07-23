FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY VulnerableApp/VulnerableApp.csproj VulnerableApp/
RUN dotnet restore VulnerableApp/VulnerableApp.csproj

COPY VulnerableApp/ VulnerableApp/
WORKDIR /src/VulnerableApp
RUN dotnet publish VulnerableApp.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Development
EXPOSE 8080

ENTRYPOINT ["dotnet", "VulnerableApp.dll"]
