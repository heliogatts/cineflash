# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy solution and project files
COPY CloudFlash.sln ./
COPY src/CloudFlash.API/CloudFlash.API.csproj src/CloudFlash.API/
COPY src/CloudFlash.Application/CloudFlash.Application.csproj src/CloudFlash.Application/
COPY src/CloudFlash.Core/CloudFlash.Core.csproj src/CloudFlash.Core/
COPY src/CloudFlash.Infrastructure/CloudFlash.Infrastructure.csproj src/CloudFlash.Infrastructure/

# Restore dependencies
RUN dotnet restore CloudFlash.sln

# Copy source code
COPY src/ src/

# Build application
RUN dotnet build CloudFlash.sln -c Release --no-restore

# Publish application
RUN dotnet publish src/CloudFlash.API/CloudFlash.API.csproj -c Release --no-build -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

# Create non-root user
RUN addgroup --system --gid 1001 dotnet \
    && adduser --system --uid 1001 --ingroup dotnet dotnet

# Copy published application
COPY --from=build /app/publish .

# Change ownership of the app directory
RUN chown -R dotnet:dotnet /app
USER dotnet

# Configure application
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
    CMD curl -f http://localhost:8080/health || exit 1

ENTRYPOINT ["dotnet", "CloudFlash.API.dll"]
