# TalentX Backend Documentation

Welcome to the comprehensive technical documentation for the **TalentX Backend** solution.

TalentX is an AI-assisted job marketplace connecting tech companies with verified AI, ML, and engineering experts. The backend is built with **C# 13 / .NET 10** following strict **Clean Architecture**, **Domain-Driven Design (DDD)**, and **CQRS (MediatR)** patterns.

---

## 📚 Documentation Navigation

```
docs/
├── README.md                          # Main Documentation Portal
│
├── architecture/                      # Architectural Patterns & System Design
│   ├── overview.md                    # Clean Architecture layers, Folder Structure & Dependency Rules
│   ├── authentication-flow.md        # JWT, Refresh Tokens, Google OAuth, OTP Verification
│   └── error-handling.md              # RFC 7807 Problem Details & Global Middleware
│
├── api/                               # REST API Endpoints Specification
│   ├── overview.md                    # Base URLs, Versioning, Request/Response Standards
│   ├── employers.md                   # Employer Registration, Google Sign-In, OTP Endpoints
│   ├── auth.md                        # User Authentication, Refresh Tokens, Cookie Sessions
│   └── health.md                      # Diagnostics & Health Monitoring
│
├── database/                          # Persistence & Database Design
│   ├── schema.md                      # EF Core Entities, Relationships & Tables
│   └── migrations.md                  # Migrations Guide (TalentX.Migrations.PostgreSQL)
│
└── setup/                             # Developer Setup & Deployment
    ├── configuration.md               # Environment Variables, AppSettings, Secrets
    └── development-guide.md           # Running Locally, Dotnet CLI Commands, Testing
```

---

## 🏛️ System Overview

| Layer | Project | Purpose | Dependencies |
| :--- | :--- | :--- | :--- |
| **Domain** | `TalentX.Domain` | Enterprise business entities (`Employer`, `User`), domain enums (`UserRole`), base models | *None* |
| **Application** | `TalentX.Application` | CQRS commands & validators, DTOs, interfaces, `Startup.cs` DI | Domain |
| **Infrastructure** | `TalentX.Infrastructure` | EF Core `ApplicationDbContext`, Identity, Auth, Mappings, Middlewares, Mailing, OTP, `Startup.cs` DI | Application, Domain |
| **Migrations** | `TalentX.Migrations.PostgreSQL` | Dedicated EF Core migrations assembly and schema snapshots | Infrastructure |
| **API / Presentation** | `TalentX.Api` | Modular REST controllers (`Admin`, `Identity`, `Registration`), `Program.cs` pipeline | Application, Infrastructure, Migrations |
| **Testing** | `TalentX.Tests` | Automated unit test suites (xUnit + Moq) | Application, Domain |

---

## 🚀 Quick Links
- **[Architectural Overview & Folder Structure](architecture/overview.md)**
- **[Authentication & Lifecycle Flow](architecture/authentication-flow.md)**
- **[API Documentation Index](api/overview.md)**
- **[Database Schema & Models](database/schema.md)**
- **[Database Migrations Guide](database/migrations.md)**
- **[Configuration & Environment](setup/configuration.md)**
- **[Local Development Guide](setup/development-guide.md)**
