# TX26-193 — Jobs Feature (Adapted to Existing Codebase)

I've read your entire codebase. Here's what I found and how the ticket maps onto it.

## Existing Codebase Summary

Your project already has a well-established architecture:

| Layer | Key Patterns |
|-------|-------------|
| **Domain** | Rich domain model (`Job.cs` with private setters, `Publish()`/`Expire()` methods, encapsulated `_skillRequirements`), `BaseEntity`, `JobEnums.cs` ✅ already matches ticket |
| **Application** | MediatR + FluentValidation + AutoMapper pipeline, `IUnitOfWork` pattern, custom exceptions, `Behaviours/` |
| **Infrastructure** | `ApplicationDbContext` (inherits `IdentityDbContext<ApplicationUser>`), PostgreSQL via Npgsql, `UnitOfWork`, repos under `Persistence/Repositories/` |
| **API** | `TalentX.Api.csproj` (namespace `TalentX.Api`), existing `GlobalExceptionMiddleware`, `RequestLoggingMiddleware`, `MiddlewareExtensions`, Swagger |

## Key Differences — Ticket vs. Your Codebase

> [!IMPORTANT]
> The ticket assumes a **different architecture** (direct service injection, no MediatR, SQL Server, a separate `AppDbContext`, namespace `TalentX.WebAPI`). Your project uses **PostgreSQL, MediatR/CQRS, UnitOfWork, IdentityDbContext, and namespace `TalentX.Api`**. I will adapt the ticket to your existing patterns instead of blindly overwriting.

| Ticket Says | Your Codebase Has | My Approach |
|---|---|---|
| `AppDbContext` (plain `DbContext`) | `ApplicationDbContext` (extends `IdentityDbContext`) | **Reuse** `ApplicationDbContext` — add `DbSet<Job>` + fluent config there |
| SQL Server (`UseSqlServer`) | PostgreSQL (`UseNpgsql`) | **Keep** PostgreSQL |
| Direct `IJobRepository` / `IJobService` injection | `IUnitOfWork` + MediatR CQRS | **Use MediatR queries** (`GetPublishedJobsQuery`, `GetJobDetailsQuery`) and add `IJobRepository` to `IUnitOfWork` |
| Namespace `TalentX.WebAPI.*` | Namespace `TalentX.Api.*` | **Keep** `TalentX.Api` |
| `ExceptionHandlingMiddleware` | `GlobalExceptionMiddleware` (richer — handles Validation, Conflict, etc.) | **Keep** existing middleware |
| Scalar API explorer | Swagger (Swashbuckle) | **Keep** Swagger |
| `Features/Jobs/JobService.cs` | MediatR handlers | **Create query handlers** instead |

## User Review Required

> [!WARNING]
> The ticket says "this supersedes every previous backend file" and suggests deleting old `Jobs/` folders. However, your existing code (`Job.cs` domain entity, enums, middleware, `UnitOfWork`, `ApplicationDbContext`) is **richer and more mature** than what the ticket proposes. I recommend **keeping everything** and adding the new files alongside it.

> [!IMPORTANT]
> Your existing `Job.cs` entity already has private setters, domain methods (`Publish()`, `Expire()`, `AddSkill()`), and a backing `_skillRequirements` field. This is **better** than the ticket's plain-property version. I'll keep your entity and only reference it from the new code.

## Open Questions

> [!IMPORTANT]
> 1. **PostgreSQL vs SQL Server** — Your infra uses Npgsql/PostgreSQL. The ticket says SQL Server. Should I keep PostgreSQL? *(I'm assuming yes.)*
> 2. **Seed data** — The ticket seeds via `Program.cs` on startup. Should I add the seed data the same way, or would you prefer a migration-based seed? *(I'll use the `Program.cs` dev-only approach as the ticket suggests.)*
> 3. **Test project location** — Your solution has `test/` (with a `.gitkeep`), but the ticket says `tests/TalentX.Tests/`. Should I use your existing `test/` folder? *(I'll use `test/TalentX.Application.Tests/`.)*

## Proposed Changes

### Domain Layer — No changes needed ✅

Your [Job.cs](file:///c:/dev/TalentX/talentx2026q3.backend/src/TalentX.Domain/Entities/Job.cs) and [JobEnums.cs](file:///c:/dev/TalentX/talentx2026q3.backend/src/TalentX.Domain/Enums/JobEnums.cs) already match (and exceed) the ticket's requirements.

---

### Application Layer

#### [NEW] `Common/PagedResult.cs`
- Generic `PagedResult<T>` class with `Items`, `Page`, `PageSize`, `TotalCount`

#### [NEW] `DTOs/Jobs/JobListItemDto.cs`
- Lightweight DTO for listing: `Id`, `Title`, `CompanyName`, `City`, `Country`, `RoleType`

#### [NEW] `DTOs/Jobs/JobDetailsDto.cs`
- Full detail DTO including `Description`, `Responsibilities`, `SkillRequirements`, etc.

#### [NEW] `Mappings/JobMappingExtensions.cs`
- `ToListItemDto()` and `ToDetailsDto()` extension methods on `Job`
- Adapts to your entity's `IReadOnlyCollection<string> SkillRequirements` (not `List<string>`)

#### [NEW] `Features/Jobs/Queries/GetPublishedJobsQuery.cs`
- MediatR `IRequest<PagedResult<JobListItemDto>>` with `Page` and `PageSize`
- Handler clamps page/pageSize, calls `IJobRepository`, maps results

#### [NEW] `Features/Jobs/Queries/GetJobDetailsQuery.cs`
- MediatR `IRequest<JobDetailsDto?>` with `Id`
- Handler calls repo, maps with `ToDetailsDto()`

#### [NEW] `Interfaces/Jobs/IJobRepository.cs`
- `GetPublishedJobsAsync(page, pageSize, ct)` → `(IReadOnlyList<Job>, int TotalCount)`
- `GetPublishedByIdAsync(id, ct)` → `Job?`

#### [MODIFY] [IUnitOfWork.cs](file:///c:/dev/TalentX/talentx2026q3.backend/src/TalentX.Application/Common/Interfaces/IUnitOfWork.cs)
- Add `IJobRepository Jobs { get; }` property

#### [MODIFY] [DependencyInjection.cs](file:///c:/dev/TalentX/talentx2026q3.backend/src/TalentX.Application/DependencyInjection.cs)
- No changes needed — MediatR auto-scans the assembly

---

### Infrastructure Layer

#### [MODIFY] [ApplicationDbContext.cs](file:///c:/dev/TalentX/talentx2026q3.backend/src/TalentX.Infrastructure/Persistence/Contexts/ApplicationDbContext.cs)
- Add `DbSet<Job> Jobs => Set<Job>();`
- Add fluent configuration for `Job` entity (max lengths, enum conversions, JSON `SkillRequirements`, indexes)
- Use `_skillRequirements` backing field for the `SkillRequirements` property

#### [NEW] `Persistence/SeedData/JobSeedData.cs`
- Static seed data with 6 jobs (4 published eligible, 1 expired, 1 draft)

#### [NEW] `Persistence/Repositories/JobRepository.cs`
- Implements `IJobRepository`
- `AsNoTracking()`, published-only + not-expired filter, pagination with `Skip/Take`, deterministic ordering

#### [MODIFY] [UnitOfWork.cs](file:///c:/dev/TalentX/talentx2026q3.backend/src/TalentX.Infrastructure/Persistence/Repositories/UnitOfWork.cs)
- Add `IJobRepository Jobs { get; }` wired to `JobRepository`

#### [MODIFY] [DependencyInjection.cs](file:///c:/dev/TalentX/talentx2026q3.backend/src/TalentX.Infrastructure/DependencyInjection.cs)
- Register `IJobRepository → JobRepository` as scoped

---

### API Layer

#### [NEW] `Controllers/JobsController.cs`
- `GET /api/jobs?page=1&pageSize=20` → dispatches `GetPublishedJobsQuery`
- `GET /api/jobs/{id:guid}` → dispatches `GetJobDetailsQuery`
- Uses `CancellationToken`, returns `404` for missing jobs
- Namespace `TalentX.Api.Controllers` (matching your existing convention)

#### [MODIFY] [appsettings.json](file:///c:/dev/TalentX/talentx2026q3.backend/src/TalentX.Api/appsettings.json)
- Add `"Cors": { "FrontendUrl": "http://localhost:3000" }`

#### [MODIFY] [Program.cs](file:///c:/dev/TalentX/talentx2026q3.backend/src/TalentX.Api/Program.cs)
- Add config-based CORS policy
- Add dev-only `db.Database.Migrate()` + seed block
- Keep existing middleware, auth, swagger — just add CORS + seed

---

### Test Project

#### [NEW] `test/TalentX.Application.Tests/TalentX.Application.Tests.csproj`
- xUnit test project referencing `TalentX.Application`

#### [NEW] `test/TalentX.Application.Tests/Jobs/GetPublishedJobsQueryTests.cs`
- Tests page clamping logic
- Tests null return for missing job

#### [NEW] `test/TalentX.Application.Tests/Jobs/GetJobDetailsQueryTests.cs`
- Tests mapping from entity to DTO
- Tests null propagation

#### [MODIFY] [TalentX.slnx](file:///c:/dev/TalentX/talentx2026q3.backend/TalentX.slnx)
- Add test project to solution

---

## Verification Plan

### Automated Tests
```bash
dotnet test test/TalentX.Application.Tests
```

### Build Verification
```bash
dotnet build
```

### Manual Verification
- `GET /api/jobs?page=1&pageSize=20` — returns only published, non-expired jobs
- `GET /api/jobs/{valid-id}` — returns full detail DTO
- `GET /api/jobs/{random-guid}` — returns 404
- Expired and Draft seed jobs never appear in results
