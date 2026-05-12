# (Runtime)
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app

# Railway öz PORT-unu təyin edir
ENV ASPNETCORE_URLS=http://+:${PORT:-8080}
EXPOSE 8080

# (SDK)
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["Basecamp-Backend.csproj", "./"]
RUN dotnet restore "Basecamp-Backend.csproj"
COPY . .
RUN dotnet build "Basecamp-Backend.csproj" -c Release -o /app/build

# (Publish)
FROM build AS publish
RUN dotnet publish "Basecamp-Backend.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Basecamp-Backend.dll"]
