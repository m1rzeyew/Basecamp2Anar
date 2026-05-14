# Stage 1 - Build
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["Basecamp-Backend.csproj", "./"]
RUN dotnet restore "Basecamp-Backend.csproj"

COPY . .

RUN dotnet publish "Basecamp-Backend.csproj" \
        --configuration Release \
        --output /app/publish \
        --no-restore

# Stage 2 - Runtime
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# ASPNETCORE_ENVIRONMENT only - do NOT hardcode ASPNETCORE_URLS or PORT.
# Railway injects PORT at runtime; Program.cs reads it via UseUrls().
ENV ASPNETCORE_ENVIRONMENT=Production

# Debian-compatible non-root user
RUN groupadd --system appgroup \
 && useradd  --system --gid appgroup --no-create-home appuser

COPY --from=build --chown=appuser:appgroup /app/publish .

USER appuser

# EXPOSE is documentation only - Railway uses the PORT env-var
EXPOSE 8080

ENTRYPOINT ["dotnet", "Basecamp-Backend.dll"]
