FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy solution and project files first for layer caching
COPY PayFlow.sln .
COPY src/PayFlow.Domain/PayFlow.Domain.csproj src/PayFlow.Domain/
COPY src/PayFlow.Application/PayFlow.Application.csproj src/PayFlow.Application/
COPY src/PayFlow.Infrastructure/PayFlow.Infrastructure.csproj src/PayFlow.Infrastructure/
COPY src/PayFlow.Api/PayFlow.Api.csproj src/PayFlow.Api/

RUN dotnet restore src/PayFlow.Api/PayFlow.Api.csproj

# Copy source and build
COPY src/ src/
RUN dotnet publish src/PayFlow.Api/PayFlow.Api.csproj \
    -c Release \
    -o /app/publish \
    --no-restore

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Security: run as non-root
RUN adduser --disabled-password --gecos "" appuser
USER appuser

COPY --from=build /app/publish .

EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

HEALTHCHECK --interval=30s --timeout=3s --start-period=10s --retries=3 \
    CMD curl -f http://localhost:8080/health/live || exit 1

ENTRYPOINT ["dotnet", "PayFlow.Api.dll"]
