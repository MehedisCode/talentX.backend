# Local Development Guide

This guide provides practical instructions for developing, building, and running automated tests for the TalentX Backend.

---

## 🛠️ Common Dotnet CLI Commands

### 1. Build the Entire Solution
```bash
dotnet build
```

### 2. Run the Web API (With Hot Reload)
```bash
dotnet watch --project src/TalentX.API
```
Or standard run:
```bash
dotnet run --project src/TalentX.API
```

### 3. Run Automated Tests
Run all test suites across `TalentX.Application.Tests` and `TalentX.Infrastructure.Tests`:
```bash
dotnet test
```

Run specific test projects:
```bash
# Run Application CQRS tests
dotnet test test/TalentX.Application.Tests

# Run Infrastructure service tests
dotnet test test/TalentX.Infrastructure.Tests
```

---

## 🗄️ Database Management

### Apply Database Migrations
```bash
dotnet ef database update --project src/TalentX.Migrations --startup-project src/TalentX.API
```

### Create a New Database Migration
```bash
dotnet ef migrations add <FeatureName> --project src/TalentX.Migrations --startup-project src/TalentX.API
```

### Drop / Reset Local Database
```bash
dotnet ef database drop --project src/TalentX.Migrations --startup-project src/TalentX.API
```

---

## 🧪 Testing Guidelines & Standards
* **Framework**: xUnit + FluentAssertions + Moq.
* **Naming Standard**: `MethodName_State_ExpectedResult` (e.g. `Handle_WhenEmailIsUnique_CreatesEmployerAndDispatchesOtp`).
* **Pattern**: Follow Arrange-Act-Assert (AAA) pattern strictly.
* **Rule**: Keep tests independent and mock external dependencies (`IIdentityService`, `IOtpService`, `IEmailService`, `IUnitOfWork`).
