# TalentX Backend Service

[![.NET 10](https://img.shields.io/badge/.NET-10.0-512BD4.svg)](https://dotnet.microsoft.com/)
[![C# 13](https://img.shields.io/badge/C%23-13.0-239120.svg)](https://learn.microsoft.com/en-us/dotnet/csharp/)
[![PostgreSQL](https://img.shields.io/badge/PostgreSQL-16+-336791.svg)](https://www.postgresql.org/)
[![Architecture](https://img.shields.io/badge/Architecture-Clean%20Architecture%20%2B%20CQRS-blue.svg)](docs/architecture/overview.md)
[![Tests](https://img.shields.io/badge/Tests-151%20Passing-brightgreen.svg)](docs/setup/development-guide.md)

TalentX is an AI-assisted job marketplace connecting tech enterprises with vetted AI, Machine Learning, and engineering experts. The backend service provides high-performance, secure REST APIs built on **.NET 10** and **C# 13** following **Clean Architecture**, **Domain-Driven Design (DDD)**, and **CQRS via MediatR**.

---

## 🏛️ Architecture & System Design

The solution strictly adheres to Clean Architecture layer separation and dependency inversion rules:

```
┌─────────────────────────────────────────────────────────────┐
│                    Presentation / API                       │
│     TalentX.API (Controllers, Swagger, Middlewares, DI)      │
└──────────────────────────────┬──────────────────────────────┘
                               │
                               ▼
┌─────────────────────────────────────────────────────────────┐
│                     Application Layer                       │
│    TalentX.Application (CQRS Commands, Queries, DTOs)       │
└──────────────────────┬─────────────────────────▲────────────┘
                       │                         │
                       ▼                         │
┌──────────────────────────────┐                 │
│         Domain Layer         │                 │
│  TalentX.Domain (Entities)   │                 │
└──────────────────────────────┘                 │
                       ▲                         │
                       │                         │
┌──────────────────────┴─────────────────────────┴────────────┐
│                    Infrastructure Layer                     │
│ TalentX.Infrastructure (EF Core, Auth, Mappings, Identity)  │
└──────────────────────────────▲──────────────────────────────┘
                               │
┌──────────────────────────────┴──────────────────────────────┐
│                      Database Migrations                    │
│      TalentX.Migrations (EF Core PostgreSQL Snapshots)      │
└─────────────────────────────────────────────────────────────┘
```

### Dependency Rules (STRICT)
* **Domain** $\rightarrow$ Depends on **nothing** (pure C# business entities, value objects, enums).
* **Application** $\rightarrow$ Depends **only on Domain** (CQRS commands, validators, query handlers).
* **Infrastructure** $\rightarrow$ Depends on **Application + Domain** (EF Core persistence, Identity, SMTP, OAuth).
* **API** $\rightarrow$ Presentation host depending on Application and Infrastructure strictly for DI wiring.
* **Migrations** $\rightarrow$ Dedicated project referencing Infrastructure for database schema versioning.

---

## 📂 Project Structure

```
talentx2026q3.backend/
├── src/
│   ├── TalentX.Domain/                     # Pure domain models (Employer, User, UserRole)
│   ├── TalentX.Application/                # CQRS features, pipeline behaviours, DTOs, validation
│   ├── TalentX.Infrastructure/             # EF Core DbContext, Identity, OAuth, MailKit, Repositories
│   ├── TalentX.Migrations/                 # Dedicated PostgreSQL EF Core migrations assembly
│   └── TalentX.API/                        # Web API entrypoint, controllers, middleware pipeline
├── test/
│   ├── TalentX.Application.Tests/          # Unit tests for Application CQRS commands & validators
│   └── TalentX.Infrastructure.Tests/       # Unit tests for Identity, TokenService, Repositories
├── docs/                                   # Full technical & architectural documentation
├── .env.example                            # Environment variables configuration template
└── TalentX.slnx                            # Solution configuration file
```

---

## 🛠️ Technology Stack

| Component | Technology | Version | Description |
| :--- | :--- | :--- | :--- |
| **Framework** | ASP.NET Core | .NET 10 | High-performance Web API |
| **Language** | C# | 13.0 | Modern C# language features |
| **Database** | PostgreSQL | 16+ | Primary relational database |
| **ORM** | Entity Framework Core | 10.0 | Code-First data access & migrations |
| **CQRS Pattern** | MediatR | 12.4 | In-process mediator for commands/queries |
| **Validation** | FluentValidation | 11.11 | Pipeline-integrated input validation |
| **Identity & Security** | ASP.NET Core Identity | 10.0 | User management, hashed passwords (PBKDF2) |
| **Authentication** | JWT Bearer + Google OAuth | RFC 7519 | Access tokens & Refresh tokens |
| **Mapping** | AutoMapper | 16.0 | DTO and entity projection |
| **Mailing** | MailKit / MimeKit | 4.10 | RFC-compliant SMTP email delivery |
| **Testing** | xUnit + FluentAssertions + Moq | Latest | 151 comprehensive unit test suites |

---

## 🚀 Getting Started

### Prerequisites
* [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
* [PostgreSQL 16+](https://www.postgresql.org/download/)
* [Entity Framework Core CLI Tools](https://learn.microsoft.com/en-us/ef/core/cli/dotnet) (`dotnet tool install --global dotnet-ef`)

### 1. Clone & Setup Environment
```bash
# Clone the repository
git clone git@bitbucket.org:saavventuresteam/talentx2026q3.backend.git
cd talentx2026q3.backend

# Copy the environment file template
cp .env.example .env
```

Edit `.env` with your PostgreSQL credentials, JWT secret, and SMTP settings.

### 2. Build the Solution
```bash
dotnet build
```

### 3. Apply Database Migrations
```bash
dotnet ef database update --project src/TalentX.Migrations --startup-project src/TalentX.API
```

### 4. Run the API Service
```bash
dotnet run --project src/TalentX.API
```

API Swagger documentation will be available at: **`http://localhost:5035/swagger`** (or configured `PORT`).

---

## 🧪 Running Automated Tests

Run the full test suite across all Application and Infrastructure test projects:

```bash
dotnet test
```

* **Application Tests**: Command handlers, validation pipelines, and business logic.
* **Infrastructure Tests**: Token service, identity service, and repository operations.

---

## 📖 Comprehensive Documentation

| Topic | Documentation Link |
| :--- | :--- |
| **Architecture Overview** | [docs/architecture/overview.md](docs/architecture/overview.md) |
| **Authentication & User Flows** | [docs/architecture/authentication-flow.md](docs/architecture/authentication-flow.md) |
| **Error Handling & Problem Details** | [docs/architecture/error-handling.md](docs/architecture/error-handling.md) |
| **API Endpoints Index** | [docs/api/overview.md](docs/api/overview.md) |
| **Employer Registration APIs** | [docs/api/employers.md](docs/api/employers.md) |
| **User Authentication APIs** | [docs/api/auth.md](docs/api/auth.md) |
| **Database Schema & Migrations** | [docs/database/migrations.md](docs/database/migrations.md) |
| **Environment Configuration** | [docs/setup/configuration.md](docs/setup/configuration.md) |
| **Developer Guide** | [docs/setup/development-guide.md](docs/setup/development-guide.md) |

---

## 📜 License
Internal & Proprietary — Saav Ventures / Astha IT. All rights reserved.
