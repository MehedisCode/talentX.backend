# Development & Testing Guide

This guide contains everything you need to build, run, test, and contribute to the TalentX Backend.

---

## 💻 Prerequisites

- **.NET 10 SDK** (`dotnet --version` >= 10.0)
- **PostgreSQL** (v14+ running locally or in Docker)
- **EF Core CLI**: `dotnet tool install --global dotnet-ef`

---

## 🚀 Running the API Locally

From the `talentx2026q3.backend/` directory:

```bash
# 1. Restore NuGet dependencies
dotnet restore

# 2. Build solution
dotnet build

# 3. Apply database migrations
dotnet ef database update -p src/TalentX.Infrastructure -s src/TalentX.Api

# 4. Run the API service
dotnet run --project src/TalentX.Api
```

- **Swagger UI**: Visit `http://localhost:5000/swagger` in your browser.
- **Health Endpoint**: `http://localhost:5000/api/v1/health`

---

## 🧪 Running Unit & Integration Tests

The test suite is built using **xUnit**, **Moq**, and **FluentAssertions**.

```bash
# Run all unit tests
dotnet test

# Run tests with detailed output
dotnet test --logger "console;verbosity=detailed"

# Run tests for specific feature (e.g. Employers)
dotnet test --filter "FullyQualifiedName~TalentX.Tests.Employers"
```

### Test Suite Structure (`test/TalentX.Tests/`):
- `Employers/CreateEmployerCommandHandlerTests.cs`: Registration, duplicate conflicts (409), unverified re-registration, Identity failures.
- `Employers/CreateEmployerCommandValidatorTests.cs`: Email formatting, password complexity, string lengths.
- `Employers/VerifyEmployerEmailCommandHandlerTests.cs`: Valid OTP verification, expired/invalid OTP, non-existent employer handling.
- `Employers/ResendEmployerOtpCommandHandlerTests.cs`: Unverified OTP resend, rejecting already-verified accounts.
- `Employers/GoogleSignInEmployerCommandHandlerTests.cs`: Valid Google token, user upserting, employer entity linking.

---

## 📦 Project Directory Layout

```
talentx2026q3.backend/
├── docs/                                  # Technical & API Documentation
│   ├── README.md
│   ├── architecture/                      # Architecture, Auth & Error specifications
│   ├── api/                               # REST API specifications
│   ├── database/                          # EF Core Schema & Migrations
│   └── setup/                             # Configuration & Developer guide
├── src/
│   ├── TalentX.Domain/                    # Domain Layer: Entities, Enums, Contracts
│   ├── TalentX.Application/               # Application Layer: CQRS, FluentValidation, DTOs
│   ├── TalentX.Infrastructure/            # Infrastructure: EF Core, Identity, External APIs
│   └── TalentX.Api/                       # API: Controllers, Program.cs, Middleware
└── test/
    └── TalentX.Tests/                     # Unit tests
```
