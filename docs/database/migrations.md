# Database Migrations Guide

TalentX utilizes a dedicated migrations assembly (**`TalentX.Migrations`**) to keep schema snapshots and generated migration scripts isolated from runtime presentation code.

---

## 🏛️ Migrations Architecture

* **Migrations Project (`-p`)**: [`src/TalentX.Migrations`](file:///Users/jubair/Developer/asthait/talentx/talentx2026q3.backend/src/TalentX.Migrations/)
* **Startup Project (`-s`)**: [`src/TalentX.API`](file:///Users/jubair/Developer/asthait/talentx/talentx2026q3.backend/src/TalentX.API/)
* **DbContext Configuration** in `PersistenceStartup.cs`:
  ```csharp
  services.AddDbContext<ApplicationDbContext>(options =>
      options.UseNpgsql(
          connectionString,
          npgsqlOptions =>
              npgsqlOptions.MigrationsAssembly("TalentX.Migrations")));
  ```

---

## 🛠️ EF Core CLI Commands

### 1. Add a New Migration
```bash
dotnet ef migrations add <MigrationName> \
  --project src/TalentX.Migrations \
  --startup-project src/TalentX.API
```

### 2. Apply Migrations to Database
```bash
dotnet ef database update \
  --project src/TalentX.Migrations \
  --startup-project src/TalentX.API
```

### 3. Remove the Last Migration (If Unapplied)
```bash
dotnet ef migrations remove \
  --project src/TalentX.Migrations \
  --startup-project src/TalentX.API
```

### 4. Generate Idempotent SQL Script (For CI/CD Pipelines)
```bash
dotnet ef migrations script \
  --project src/TalentX.Migrations \
  --startup-project src/TalentX.API \
  --idempotent \
  --output migrations.sql
```

---

## ⚠️ Important Guidelines
* **Never manually edit** generated `.Designer.cs` or `ApplicationDbContextModelSnapshot.cs` files unless resolving a Git merge conflict.
* Always build the solution (`dotnet build`) before running migration commands.
* Ensure your `.env` contains valid PostgreSQL credentials before running `database update`.
