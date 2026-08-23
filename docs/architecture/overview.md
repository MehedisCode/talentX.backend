# Architecture Overview

The TalentX Backend is designed using **Clean Architecture** principles and **CQRS (Command Query Responsibility Segregation)** with MediatR. This ensures that the core business domain is completely decoupled from database technologies, UI frameworks, and third-party services.

---

## 🏗️ Layered Architecture Diagram

```
                       ┌───────────────────────────────┐
                       │          TalentX.API          │
                       │   (Controllers, Routing)      │
                       └──────────────┬────────────────┘
                                      │
                                      ▼
                       ┌───────────────────────────────┐
                       │      TalentX.Application      │
                       │ (Commands, DTOs, Validators)  │
                       └──────┬─────────────────▲──────┘
                              │                 │
                              ▼                 │
    ┌─────────────────────────────┐             │
    │       TalentX.Domain        │             │
    │ (Entities, Enums, Contracts)│             │
    └─────────────────────────────┘             │
                              ▲                 │
                              │                 │
                       ┌──────┴─────────────────┴──────┐
                       │     TalentX.Infrastructure    │
                       │(EF Core, Auth, Mappings, Mids)│
                       └──────────────▲────────────────┘
                                      │
                       ┌──────────────┴────────────────┐
                       │       TalentX.Migrations      │
                       │ (Dedicated Migrations Project)│
                       └───────────────────────────────┘
```

---

## 📁 Solution Project & Folder Structure

```
talentx2026q3.backend/
├── src/
│   ├── TalentX.Domain/                     # Pure enterprise domain layer
│   │   ├── Common/                         # Base entities & shared domain models
│   │   │   ├── BaseEntity.cs               # Audit fields (Id, CreatedAt, UpdatedAt)
│   │   │   └── Enums/                      # Domain enums (UserRole: Employer, Talent)
│   │   │       └── UserRole.cs
│   │   ├── Entities/
│   │   │   └── Job.cs                      # Job listing domain entity (Draft/Published lifecycle)
│   │   ├── Employer.cs                     # Root Employer domain entity
│   │   └── User.cs                         # Root User domain entity
│   │
│   ├── TalentX.Application/                # CQRS application business logic
│   │   ├── Common/                         # Cross-cutting application interfaces & pipelines
│   │   │   ├── Behaviours/                 # MediatR pipeline behaviours (Logging, Validation)
│   │   │   ├── Exceptions/                 # Domain/App exceptions (AppException, Conflict, etc.)
│   │   │   └── Interfaces/                 # Service & repository contracts (IUnitOfWork, IJobRepository, IEmployerRepository)
│   │   ├── DTOs/                           # Data Transfer Objects (Employers)
│   │   ├── Features/                       # Feature slices organized by domain area
│   │   │   ├── Auth/                       # CurrentUser, Login, Logout, RefreshToken handlers & validators
│   │   │   ├── Employers/Commands/         # CreateEmployer, GoogleSignIn, ResendOtp, VerifyEmail
│   │   │   └── Jobs/Commands/               # CreateJob, UpdateJob handlers & validators
│   │   ├── Identity/                       # Identity contracts & models (IIdentityService, ITokenService)
│   │   ├── ApplicationStartup.cs           # Application DI service registration extensions (AddApplication)
│   │   └── GlobalUsings.cs                 # Application-wide global using directives
│   │
│   ├── TalentX.Infrastructure/             # Technical capabilities & external implementations
│   │   ├── Auth/                           # External auth (GoogleAuthService, TokenService)
│   │   ├── Common/                         # Cross-cutting infrastructure services (OtpService)
│   │   ├── Identity/                       # ASP.NET Core Identity setup, models & seeders
│   │   │   ├── Configurations/             # RefreshToken entity configuration
│   │   │   ├── Models/                     # ApplicationUser, RefreshToken, JwtSettings
│   │   │   ├── Seed/                       # IdentitySeeder (Roles, Initial Admin)
│   │   │   ├── Services/                   # IdentityService, ExternalIdentityService, CurrentUserService
│   │   │   └── IdentityStartup.cs          # Identity DI extension (AddIdentityServices)
│   │   ├── Mailing/                        # MailKit SMTP email service & settings
│   │   ├── Mappings/                       # AutoMapper profiles (EmployerMappingProfile)
│   │   ├── Middlewares/                    # HTTP middlewares (ExceptionMiddleware, RequestLoggingMiddleware)
│   │   │   └── MiddlewareStartup.cs        # Middleware pipeline extension (UseInfrastructureMiddlewares)
│   │   ├── Persistence/                    # EF Core database access
│   │   │   ├── Configurations/             # Entity type configurations (Job, Employer, User, RefreshToken)
│   │   │   ├── Contexts/                   # ApplicationDbContext
│   │   │   ├── Repositories/               # JobRepository, EmployerRepository, UserRepository, UnitOfWork
│   │   │   └── PersistenceStartup.cs       # Persistence DI extension (AddPersistence)
│   │   ├── InfrastructureStartup.cs        # Infrastructure master DI extension (AddInfrastructure)
│   │   └── GlobalUsings.cs                 # Infrastructure-wide global using directives
│   │
│   ├── TalentX.Migrations/                 # Dedicated database migrations assembly
│   │   ├── Migrations/                     # EF Core migration snapshots and historical scripts
│   │   └── TalentX.Migrations.csproj
│   │
│   └── TalentX.API/                        # Presentation & HTTP API entry point
│       ├── Controllers/                    # HTTP Controllers organized by module
│       │   ├── Auth/                       # Authentication endpoints (AuthController)
│       │   ├── Employers/                  # Registration & onboarding (EmployersController)
│       │   ├── Jobs/                       # Job listing endpoints (JobsController)
│       │   ├── BaseApiController.cs        # Base controller injecting ISender
│       │   └── HealthController.cs         # Administrative & diagnostic endpoints
│       ├── Swagger/                        # OpenAPI / Swagger configuration (SwaggerStartup)
│       ├── Program.cs                      # Application bootstrapping & middleware configuration
│       ├── TalentX.API.csproj
│       └── appsettings.json                # Runtime configuration settings
│
└── test/
    ├── TalentX.Domain.Tests/                # Automated unit tests for domain entities
    │   └── Entities/                        # JobTests
    ├── TalentX.Application.Tests/          # Automated unit tests for CQRS features & validation
    │   └── Features/                       # Unit tests for Auth, Employer & Job CQRS commands
    ├── TalentX.Infrastructure.Tests/       # Automated unit tests for Identity, TokenService & Repositories
    │   └── Identity/Services/              # IdentityServiceTests, TokenServiceTests
    └── TalentX.Api.Tests/                   # Automated unit tests for controllers
        └── Controllers/Jobs/                # JobsControllerTests
```

---

## 🧱 Dependency Inversion Rules (STRICT)

1. **Domain (`TalentX.Domain`)**:
   - The absolute center of the application.
   - Depends on **nothing** (no external NuGet packages, no database dependencies).
   - Contains pure enterprise models (`User`, `Employer`, `Job`), domain enums (`UserRole`, `JobStatus`, `EmploymentType`, `ExperienceLevel`, `WorkArrangement`), and base entity classes.

2. **Application (`TalentX.Application`)**:
   - Depends **only on Domain**.
   - Implements CQRS via MediatR:
     - **Commands** modify state (e.g. `CreateEmployerCommand`, `GoogleSignInEmployerCommand`, `VerifyEmployerEmailCommand`, `ResendEmployerOtpCommand`, `LoginCommand`, `LogoutCommand`, `RefreshTokenCommand`, `CreateJobCommand`, `UpdateJobCommand`).
     - **Queries** read state (e.g. `CurrentUserQuery`).
   - Validates all input commands using **FluentValidation** before execution.
   - Defines abstract repository and service interfaces (`IUnitOfWork`, `IJobRepository`, `IEmployerRepository`, `IIdentityService`, `ITokenService`).

3. **Infrastructure (`TalentX.Infrastructure`)**:
   - Implements the interfaces defined in the Application layer.
   - Houses Entity Framework Core `ApplicationDbContext`, entity configurations, and repository implementations.
   - Encapsulates technical implementations:
     - **Auth**: ASP.NET Core Identity (`ApplicationUser`), JWT token generation, refresh token management.
     - **OAuth**: Google ID Token validation via `GoogleJsonWebSignature`.
     - **Mailing**: SMTP email service via MailKit / MimeKit.
     - **Common**: Thread-safe in-memory OTP engine.
     - **Mappings**: AutoMapper profile configurations.
     - **Middleware**: Exception handling (`ExceptionMiddleware`) and structured request logging.

4. **Migrations (`TalentX.Migrations`)**:
   - Dedicated EF Core migrations project maintaining database migration history.
   - Decoupled from application runtime logic to support streamlined schema evolutions.

5. **API (`TalentX.API`)**:
   - The presentation entry point.
   - Configures Dependency Injection (DI) via layer `*Startup.cs` extensions, registers middleware pipelines, CORS, and OpenAPI.
   - Contains slim controllers inheriting from `BaseApiController`.

---

## ⚡ CQRS Request Execution Pipeline

When an HTTP request enters the system:

```
[ HTTP Request ]
       │
       ▼
1. Controller (e.g. EmployersController / AuthController / JobsController) receives DTO
       │
       ▼
2. MediatR Pipeline: LoggingBehaviour (Logs execution & duration)
       │
       ▼
3. MediatR Pipeline: ValidationBehaviour (Runs FluentValidation)
       │   └── (If invalid, throws FluentValidation.ValidationException -> 400 Bad Request)
       ▼
4. Command / Query Handler executes business logic:
       ├── Modifies tracked Domain Entities
       ├── Uses Repositories via IUnitOfWork
       └── Calls _unitOfWork.SaveChangesAsync()
       │
       ▼
5. Controller returns result / DTO with appropriate HTTP status (200 OK, 201 Created)
```
