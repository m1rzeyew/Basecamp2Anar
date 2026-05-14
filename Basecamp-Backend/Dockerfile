# ─────────────────────────────────────────────────────────────────
#  Stage 1 – Restore & build
#  Use the SDK image matching your TargetFramework (net10.0)
# ─────────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy only the project file first – maximises Docker layer caching
COPY ["Basecamp-Backend.csproj", "./"]
RUN dotnet restore "Basecamp-Backend.csproj"

# Copy the rest of the source code
COPY . .

# Publish a self-contained, release-optimised binary
RUN dotnet publish "Basecamp-Backend.csproj" \
        --configuration Release \
        --output /app/publish \
        --no-restore

# ─────────────────────────────────────────────────────────────────
#  Stage 2 – Runtime image (minimal, no SDK)
# ─────────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# Railway sets PORT dynamically – the app reads it at startup
ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://0.0.0.0:8080

# Debian-compatible non-root user  (aspnet image is Debian, NOT Alpine)
# Alpine uses: addgroup / adduser
# Debian uses: groupadd / useradd   ← correct for mcr.microsoft.com/dotnet/aspnet
RUN groupadd --system appgroup \
 && useradd  --system --gid appgroup --no-create-home appuser

COPY --from=build --chown=appuser:appgroup /app/publish .

USER appuser

# Expose the default port (Railway overrides via PORT env-var)
EXPOSE 8080

ENTRYPOINT ["dotnet", "Basecamp-Backend.dll"]
