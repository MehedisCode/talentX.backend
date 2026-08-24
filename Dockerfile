# =========================
# Build stage
# =========================
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

WORKDIR /src

COPY TalentX.slnx ./
COPY src/TalentX.API/TalentX.API.csproj src/TalentX.API/
COPY src/TalentX.Application/TalentX.Application.csproj src/TalentX.Application/
COPY src/TalentX.Domain/TalentX.Domain.csproj src/TalentX.Domain/
COPY src/TalentX.Infrastructure/TalentX.Infrastructure.csproj src/TalentX.Infrastructure/
COPY src/TalentX.Migrations/TalentX.Migrations.csproj src/TalentX.Migrations/

RUN dotnet restore TalentX.slnx

COPY src/ ./src/

# =========================
# Publish stage
# =========================
FROM build AS publish

RUN dotnet publish src/TalentX.API/TalentX.API.csproj \
    --configuration Release \
    --no-restore \
    --output /app/publish \
    /p:UseAppHost=false

# =========================
# Runtime stage
# =========================
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime

WORKDIR /app

ENV ASPNETCORE_HTTP_PORTS=5035
EXPOSE 5035

COPY --from=publish /app/publish .

USER $APP_UID

ENTRYPOINT ["dotnet", "TalentX.API.dll"]