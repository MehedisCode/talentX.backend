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
COPY test/TalentX.Domain.Tests/TalentX.Domain.Tests.csproj test/TalentX.Domain.Tests/
COPY test/TalentX.Application.Tests/TalentX.Application.Tests.csproj test/TalentX.Application.Tests/
COPY test/TalentX.Infrastructure.Tests/TalentX.Infrastructure.Tests.csproj test/TalentX.Infrastructure.Tests/
COPY test/TalentX.Api.Tests/TalentX.Api.Tests.csproj test/TalentX.Api.Tests/

RUN dotnet restore TalentX.slnx

COPY src/ ./src/

# =========================
# Publish stage
# =========================
FROM build AS publish

RUN dotnet publish src/TalentX.API/TalentX.API.csproj \
    --configuration Release \
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