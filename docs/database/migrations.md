# Database Migrations Guide

TalentX utilizes Entity Framework Core with a dedicated migration project: `TalentX.Migrations.PostgreSQL`.

---

## 🏗️ Architecture

```
src/
  TalentX.Domain/                 → Core entities (Employer, User) & Enums
  TalentX.Application/            → CQRS Features, DTOs, Behaviors, Startup.cs
  TalentX.Infrastructure/         → DbContext, Repositories, Auth, Mapping, Startup.cs
  TalentX.Migrations.PostgreSQL/  → EF Core Migrations & Snapshots
  TalentX.Api/                    → REST Controllers, Hosted Services, Program.cs
```

---

## 🚀 Common Commands

All commands should be executed from the backend root folder (`talentx2026q3.backend/`):

### 1. Add a New Migration
```bash
dotnet ef migrations add <MigrationName> -p src/TalentX.Migrations.PostgreSQL -s src/TalentX.Api
```

### 2. Apply Migrations to Local / Remote Database
```bash
dotnet ef database update -p src/TalentX.Migrations.PostgreSQL -s src/TalentX.Api
```

### 3. Remove the Last Unapplied Migration
```bash
dotnet ef migrations remove -p src/TalentX.Migrations.PostgreSQL -s src/TalentX.Api
```

### 4. Generate SQL Script for Production Deployment
```bash
dotnet ef migrations script -p src/TalentX.Migrations.PostgreSQL -s src/TalentX.Api -i -o script.sql
```

---

## ⚙️ Configuration

- **DbContext**: Configured in `TalentX.Infrastructure.Startup.AddInfrastructureServices`.
- **Migrations Assembly**: Configured via `.MigrationsAssembly("TalentX.Migrations.PostgreSQL")`.
- **Automatic Migration**: Development startup automatically runs `db.Database.Migrate()` in `Program.cs`.
