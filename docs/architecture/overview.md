# Architecture Overview

The TalentX Backend is designed using **Clean Architecture** principles and **CQRS (Command Query Responsibility Segregation)** with MediatR. This ensures that the core business domain is completely decoupled from database technologies, UI frameworks, and third-party services.

---

## 🏗️ Layered Architecture Diagram

```
                       ┌───────────────────────────────┐
                       │          TalentX.Api          │
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
                       │ TalentX.Migrations.PostgreSQL │
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
│   │   │   └── Enums/                      # Domain enums grouped by category
│   │   │       └── UserEnums/
│   │   │           └── UserRole.cs         # Platform user roles (Employer, Talent)
│   │   ├── Employer.cs                     # Root Employer domain entity
│   │   └── User.cs                         # Root User domain entity
│   │
│   ├── TalentX.Application/                # CQRS application business logic
│   │   ├── Common/                         # Cross-cutting application interfaces & pipelines
│   │   │   ├── Behaviours/                 # MediatR pipeline behaviours (Logging, Validation)
│   │   │   ├── Exceptions/                 # Domain/App exceptions (Conflict, NotFound, etc.)
│   │   │   ├── Identity/                   # Identity interfaces & models (ITokenService, etc.)
│   │   │   └── Interfaces/                 # Service & repository contracts
│   │   │       └── Repositories/           # IEmployerRepository, IUserRepository, IUnitOfWork
│   │   ├── DTOs/                           # Data Transfer Objects
│   │   │   └── Employers/                  # EmployerDto
│   │   ├── Features/                       # Feature slices organized by domain area
│   │   │   ├── Auth/                       # Login, Logout, RefreshToken handlers & validators
│   │   │   └── Employers/Commands/         # CreateEmployer, GoogleSignIn, ResendOtp, VerifyEmail
│   │   ├── GlobalUsings.cs                 # Application-wide global using directives
│   │   └── Startup.cs                      # Application DI service registration extensions
│   │
│   ├── TalentX.Infrastructure/             # Technical capabilities & external implementations
│   │   ├── Auth/                           # Auth services (CurrentUserService, GoogleAuthService, TokenService, JwtSettings)
│   │   ├── Common/                         # Cross-cutting infrastructure services (OtpService)
│   │   ├── Identity/                       # ASP.NET Core Identity setup, models & seeders
│   │   │   ├── Configurations/             # RefreshToken entity configuration
│   │   │   ├── Models/                     # ApplicationUser, RefreshToken
│   │   │   ├── Seed/                       # IdentitySeeder (Roles, Initial Admin)
│   │   │   └── Services/                   # IdentityService, ExternalIdentityService
│   │   ├── Mailing/                        # MailKit SMTP email service & settings
│   │   ├── Mappings/                       # AutoMapper profiles (EmployerMappingProfile)
│   │   ├── Middleware/                     # HTTP middlewares (GlobalException, RequestLogging) & extensions
│   │   ├── Persistence/                    # EF Core database access
│   │   │   ├── Configurations/             # Entity type configurations (Employer, User)
│   │   │   ├── Contexts/                   # ApplicationDbContext
│   │   │   └── Repositories/               # EmployerRepository, UserRepository, UnitOfWork
│   │   ├── GlobalUsings.cs                 # Infrastructure-wide global using directives
│   │   └── Startup.cs                      # Infrastructure DI & DbContext assembly registration
│   │
│   ├── TalentX.Migrations.PostgreSQL/      # Dedicated database migrations assembly
│   │   └── Migrations/                     # EF Core migration snapshots and historical scripts
│   │
│   └── TalentX.Api/                        # Presentation & HTTP API entry point
│       ├── Controllers/                    # HTTP Controllers organized by module
│       │   ├── Admin/                      # Administrative & diagnostic endpoints (HealthController)
│       │   ├── Identity/                   # Authentication endpoints (AuthController)
│       │   ├── Registration/               # Registration & onboarding (EmployersController)
│       │   ├── BaseApiController.cs        # Base controller injecting ISender Mediator
│       │   └── VersionedApiController.cs   # Versioned API controller with [Route("api/v1/[controller]")]
│       ├── GlobalUsings.cs                 # API-wide global using directives
│       ├── Program.cs                      # Application bootstrapping & middleware configuration
│       └── appsettings.json                # Runtime configuration settings
│
└── test/
    └── TalentX.Tests/                      # Automated unit testing suite
        └── Employers/                      # Unit tests for Employer CQRS commands & validators
```

---

## 🧱 Dependency Inversion Rules (STRICT)

1. **Domain (`TalentX.Domain`)**:
   - The absolute center of the application.
   - Depends on **nothing** (no external NuGet packages, no database dependencies).
   - Contains pure enterprise models (`User`, `Employer`), domain enums (`UserRole`), and base entity classes.

2. **Application (`TalentX.Application`)**:
   - Depends **only on Domain**.
   - Implements CQRS via MediatR:
     - **Commands** modify state (e.g. `CreateEmployerCommand`, `GoogleSignInEmployerCommand`, `VerifyEmployerEmailCommand`, `ResendEmployerOtpCommand`).
     - **Queries** read state.
   - Validates all input commands using **FluentValidation** before execution.
   - Defines abstract repository and service interfaces (`IUnitOfWork`, `IEmployerRepository`, `IOtpService`, `IIdentityService`).

3. **Infrastructure (`TalentX.Infrastructure`)**:
   - Implements the interfaces defined in the Application layer.
   - Houses Entity Framework Core `ApplicationDbContext`, entity configurations, and repository implementations.
   - Encapsulates technical implementations:
     - **Auth**: ASP.NET Core Identity (`ApplicationUser`), JWT token generation, refresh token management.
     - **OAuth**: Google ID Token validation via `GoogleJsonWebSignature`.
     - **Mailing**: SMTP email service via MailKit / MimeKit.
     - **Common**: Thread-safe in-memory OTP engine.
     - **Mappings**: AutoMapper profile configurations.
     - **Middleware**: Global exception handling and structured request logging.

4. **Migrations (`TalentX.Migrations.PostgreSQL`)**:
   - Dedicated EF Core migrations project maintaining database migration history.
   - Decoupled from application runtime logic to support streamlined schema evolutions.

5. **API (`TalentX.Api`)**:
   - The presentation entry point.
   - Configures Dependency Injection (DI) via layer `Startup.cs` extensions, registers middleware pipelines from `TalentX.Infrastructure.Middleware`, CORS, and OpenAPI.
   - Contains slim controllers grouped by feature module inheriting from `VersionedApiController`.

---

## ⚡ CQRS Request Execution Pipeline

When an HTTP request enters the system:

```
[ HTTP Request ]
       │
       ▼
1. Controller (e.g. EmployersController) receives DTO
       │
       ▼
2. MediatR Pipeline: LoggingBehaviour (Logs execution & duration)
       │
       ▼
3. MediatR Pipeline: ValidationBehaviour (Runs FluentValidation)
       │   └── (If invalid, throws FluentValidation.ValidationException -> 400 Bad Request)
       ▼
4. Command Handler executes business logic:
       ├── Modifies tracked Domain Entities
       ├── Uses Repositories via IUnitOfWork
       └── Calls _unitOfWork.SaveChangesAsync()
       │
       ▼
5. Controller returns mapped DTO with appropriate HTTP status (200 OK, 201 Created)
```
