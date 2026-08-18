# Architecture Overview

The TalentX Backend is designed using **Clean Architecture** principles and **CQRS (Command Query Responsibility Segregation)** with MediatR. This ensures that the core business domain is completely decoupled from database technologies, UI frameworks, and third-party services.

---

## 🏗️ Layered Architecture Diagram

```
                       ┌───────────────────────────────┐
                       │          TalentX.Api          │
                       │   (Controllers, Middlewares)  │
                       └──────────────┬────────────────┘
                                      │
                                      ▼
                       ┌───────────────────────────────┐
                       │      TalentX.Application      │
                       │ (Commands, Queries, Mappings) │
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
                       │  (EF Core, Identity, External)│
                       └───────────────────────────────┘
```

---

## 🧱 Dependency Inversion Rules (STRICT)

1. **Domain (`TalentX.Domain`)**:
   - The absolute center of the application.
   - Depends on **nothing** (no external NuGet packages, no database dependencies).
   - Contains pure enterprise models (`User`, `Employer`, `Job`), domain enums (`UserRole`, `JobStatus`), and base entity classes.

2. **Application (`TalentX.Application`)**:
   - Depends **only on Domain**.
   - Implements CQRS via MediatR:
     - **Commands** modify state (e.g. `CreateEmployerCommand`, `VerifyEmployerEmailCommand`).
     - **Queries** read state (e.g. `GetPublishedJobsQuery`, `GetJobDetailsQuery`).
   - Validates all input commands using **FluentValidation** before execution.
   - Defines abstract repository and service interfaces (`IUnitOfWork`, `IEmployerRepository`, `IOtpService`, `IIdentityService`).

3. **Infrastructure (`TalentX.Infrastructure`)**:
   - Implements the interfaces defined in the Application layer.
   - Houses Entity Framework Core `ApplicationDbContext`, entity configurations, migrations, and repository implementations.
   - Encapsulates external service integrations:
     - **Identity & Security**: ASP.NET Core Identity (`ApplicationUser`), JWT token generation, refresh token management.
     - **OAuth**: Google ID Token validation via `GoogleJsonWebSignature`.
     - **Mailing**: SMTP email service via MailKit / MimeKit.
     - **OTP**: Thread-safe in-memory OTP engine.

4. **API (`TalentX.Api`)**:
   - The presentation entry point.
   - Configures Dependency Injection (DI), middleware pipelines (`GlobalExceptionMiddleware`), CORS, and OpenAPI (Swagger).
   - Contains slim controllers that delegate directly to MediatR via `ISender`.

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
