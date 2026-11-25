# syntax=docker/dockerfile:1

# ---------- Build stage: full .NET SDK, compiles and publishes the API ----------
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /source

# Copy only the project file first and restore NuGet packages.
# Docker caches this layer, so packages are re-downloaded only when the .csproj changes.
COPY src/SmartFlow.Api/SmartFlow.Api.csproj src/SmartFlow.Api/
RUN dotnet restore src/SmartFlow.Api/SmartFlow.Api.csproj

# Copy the source code and publish a Release build.
COPY src/SmartFlow.Api/ src/SmartFlow.Api/
RUN dotnet publish src/SmartFlow.Api/SmartFlow.Api.csproj -c Release -o /app --no-restore

# ---------- Runtime stage: small image with only the ASP.NET Core runtime ----------
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# Folder for the SQLite database (mounted as a Docker volume), owned by the non-root user.
RUN mkdir -p /app/data && chown app:app /app/data

COPY --from=build /app ./

# Run as the built-in non-root "app" user instead of root.
USER app

EXPOSE 8080
ENTRYPOINT ["dotnet", "SmartFlow.Api.dll"]
