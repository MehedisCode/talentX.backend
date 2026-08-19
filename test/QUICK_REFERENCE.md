# Authentication Tests - Quick Reference Cheat Sheet

## Test Execution Quick Commands

```bash
# Run all tests
dotnet test

# Run specific test project
dotnet test test/TalentX.Application.Tests/TalentX.Application.Tests.csproj

# Run specific test class
dotnet test --filter "ClassName=LoginCommandHandlerTests"

# Run with verbose output
dotnet test --verbosity detailed

# Run with code coverage
dotnet test /p:CollectCoverage=true

# Run and stop on first failure
dotnet test --no-build -x
```

## VS Code Test Explorer

| Action | Command |
|--------|---------|
| Open Test Explorer | `Ctrl+E, T` or `Test > Test Explorer` |
| Run All Tests | Click "Run All" button in Test Explorer |
| Run Single Test | Right-click test → "Run Test" |
| Debug Single Test | Right-click test → "Debug Test" |
| Run Failed Tests | Click "Run Failed Tests" button |

## Common Test Assertions (FluentAssertions)

```csharp
// Boolean assertions
result.Success.Should().BeTrue();
result.Success.Should().BeFalse();

// String assertions
result.Errors.Should().Contain("Invalid credentials");
result.Data.Email.Should().Be("user@example.com");
result.Data.Email.Should().NotBeNullOrEmpty();

// Collection assertions
result.Errors.Should().BeEmpty();
result.Errors.Should().HaveCount(3);
result.Errors.Should().Contain("Error 1");
result.Errors.Should().HaveCountGreaterThanOrEqualTo(2);

// Null assertions
result.Data.Should().BeNull();
result.Data.Should().NotBeNull();

// Exception assertions
await Assert.ThrowsAsync<ArgumentNullException>(() => handler.Handle(null));
```

## Common Mock Patterns

### Setup Mock to Return Value
```csharp
_serviceMock
    .Setup(x => x.Method(email, password))
    .ReturnsAsync(expectedResult);
```

### Setup with Any Parameters
```csharp
_serviceMock
    .Setup(x => x.Method(It.IsAny<string>(), It.IsAny<string>()))
    .ReturnsAsync(expectedResult);
```

### Setup with Callback
```csharp
_serviceMock
    .Setup(x => x.Method(It.IsAny<ApplicationUser>(), password))
    .Callback<ApplicationUser, string>((u, p) => u.Id = "test-id")
    .ReturnsAsync(IdentityResult.Success);
```

### Verify Method Was Called
```csharp
// Called exactly once
_serviceMock.Verify(x => x.Method(email, password), Times.Once);

// Called exactly N times
_serviceMock.Verify(x => x.Method(), Times.Exactly(3));

// Never called
_serviceMock.Verify(x => x.Method(), Times.Never);

// Called at least once
_serviceMock.Verify(x => x.Method(), Times.AtLeastOnce);
```

## Test Class Template

```csharp
using FluentAssertions;
using Moq;
using Xunit;

namespace TalentX.Application.Tests.Features.Auth.YourFeature;

public class YourFeatureHandlerTests
{
    private readonly Mock<IDependency> _dependencyMock;
    private readonly YourFeatureHandler _handler;

    public YourFeatureHandlerTests()
    {
        _dependencyMock = new Mock<IDependency>();
        _handler = new YourFeatureHandler(_dependencyMock.Object);
    }

    #region Happy Path

    [Fact]
    public async Task Handle_WithValidInput_ShouldReturnSuccess()
    {
        // Arrange
        var command = new YourCommand("test");
        _dependencyMock
            .Setup(x => x.Method(It.IsAny<string>()))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        _dependencyMock.Verify(x => x.Method("test"), Times.Once);
    }

    #endregion

    #region Error Conditions

    [Fact]
    public async Task Handle_WithInvalidInput_ShouldReturnFailure()
    {
        // Arrange
        var command = new YourCommand("invalid");
        _dependencyMock
            .Setup(x => x.Method(It.IsAny<string>()))
            .ReturnsAsync(IdentityResponse.Fail("Error message"));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Success.Should().BeFalse();
        result.Errors.Should().Contain("Error message");
    }

    #endregion
}
```

## Test Data Constants

```csharp
// Valid Test Data
const string ValidEmail = "testuser@example.com";
const string ValidPassword = "ValidPassword123!";
const string ValidUserId = "user-123";
const string ValidAccessToken = "access.token.value";
const string ValidRefreshToken = "refresh.token.value";

// Invalid Test Data
const string InvalidEmail = "invalid-email";
const string InvalidPassword = "weak";
const string InvalidToken = "invalid.token";

// Edge Cases
const string BoundaryEmail = "a@b.c";
const string VeryLongEmail = "verylongemailaddress@verylongdomainname.extension";
const string EmptyString = "";
```

## Test Naming Convention

```csharp
// Format: Method_Scenario_ExpectedResult

// Good Examples
public async Task Handle_WithValidCredentials_ShouldReturnSuccessfulLoginResult()
public async Task Handle_WithInvalidPassword_ShouldReturnFailureResult()
public async Task Validate_WithEmptyEmail_ShouldFail()
public async Task CreateApplicationUserAsync_WithDuplicateEmail_ShouldReturnFailure()

// Variations
public async Task Handle_ShouldCallDependencyMethod_OnceWithCorrectParameter()
public async Task Handle_WithLockedOutAccount_ShouldNotCallTokenService()
```

## Test Organization Regions

```csharp
#region Happy Path Tests
// Successful scenarios

#region Error Condition Tests
// Various failure scenarios

#region Data Integrity Tests
// Verify data preservation

#region Boundary Value Tests
// Min/max input values

#region Cancellation Tests
// CancellationToken handling

#region Async Tests
// Asynchronous behavior
```

## Running Specific Test Scenarios

```bash
# Run all login tests
dotnet test --filter "ClassName~Login"

# Run all handler tests (not validators)
dotnet test --filter "ClassName~Handler"

# Run happy path tests (by naming convention)
dotnet test --filter "MethodName~_WithValid_"

# Run error condition tests
dotnet test --filter "MethodName~_ShouldReturnFailure"

# Run tests in specific project
dotnet test test/TalentX.Application.Tests/TalentX.Application.Tests.csproj
```

## Debug a Failing Test

```bash
# Run with detailed output
dotnet test --verbosity detailed

# Run specific test in debug mode
dotnet test --filter "ClassName=LoginCommandHandlerTests" --logger console -vvv

# In VS, right-click test and select "Debug Test"

# Check test output for assertions that failed
# Look for FluentAssertions error messages
```

## Test File Organization

```
Features/
└── Auth/
    ├── Login/
    │   ├── LoginCommandHandlerTests.cs
    │   └── LoginCommandValidatorTests.cs
    ├── RefreshToken/
    │   └── RefreshTokenCommandHandlerTests.cs
    └── Logout/
        └── LogoutCommandHandlerTests.cs

Identity/
└── Services/
    └── IdentityServiceTests.cs
```

## Creating Test Classes for New Features

1. **Create folder structure**: `Features/Auth/YourFeature/`
2. **Create test class**: `YourFeatureHandlerTests.cs` or `YourFeatureValidatorTests.cs`
3. **Add constructor**: Initialize handler/validator and mock dependencies
4. **Organize with regions**: Happy Path, Error Conditions, Edge Cases, etc.
5. **Follow naming**: `Handle_Scenario_ExpectedResult()`
6. **Run tests**: `dotnet test`

## Common Test Failures & Quick Fixes

| Error | Cause | Fix |
|-------|-------|-----|
| `NullReferenceException` | Uninitialized mock/handler | Initialize in constructor |
| `Expected 1 but was 0 calls` | Mock method not called | Verify mock setup matches |
| `Test timed out` | Async/await issue | Add `await` to async calls |
| `Cannot convert null to type` | Mock returns null | Setup mock to return value |
| `Verification failed` | Different parameters passed | Use `It.IsAny<>()` or check values |

## Performance Optimization Tips

```csharp
// ✅ Fast: Use mocks
_mock.Setup(x => x.Method()).ReturnsAsync(result);

// ❌ Slow: Avoid actual calls
// Don't call real database or services

// ✅ Fast: Avoid delays
// Don't use Task.Delay in unit tests

// ✅ Fast: Parallel test execution
// xUnit runs tests in parallel by default
```

## Test Project Dependencies Quick Reference

```xml
<!-- Minimum required -->
<PackageReference Include="xunit" Version="2.4.2" />
<PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.9.0" />
<PackageReference Include="Moq" Version="4.20.70" />
<PackageReference Include="FluentAssertions" Version="6.12.0" />

<!-- Restore with -->
dotnet restore
```

## Common Keyboard Shortcuts (VS)

| Action | Shortcut |
|--------|----------|
| Run All Tests | `Ctrl+E, Ctrl+T` |
| Open Test Explorer | `Test > Test Explorer` |
| Go to Definition | `F12` |
| Find References | `Shift+F12` |
| Extract Method | `Ctrl+R, Ctrl+M` |
| Format Document | `Ctrl+K, Ctrl+D` |

## Quick Test Statistics

| Metric | Value |
|--------|-------|
| Total Tests | 72 |
| Login Tests | 27 |
| Refresh Tests | 9 |
| Logout Tests | 10 |
| Identity Service Tests | 26 |
| Average Test Duration | < 100ms |
| Coverage Target | > 90% |

## Documentation References

- **TESTING_GUIDE.md**: Comprehensive testing patterns and best practices
- **TEST_SCENARIOS.md**: Detailed test scenarios and business logic
- **README.md**: Overview and getting started guide

## Useful Commands Summary

```bash
# Quick test run
dotnet test

# Verbose output with timing
dotnet test --verbosity detailed

# Run with coverage
dotnet test /p:CollectCoverage=true

# Run single test
dotnet test --filter "FullyQualifiedName~LoginCommandHandlerTests.Handle_WithValidCredentials"

# Run and show results in console
dotnet test --logger console

# Clean and rebuild
dotnet clean && dotnet build && dotnet test
```

---

**Last Updated**: 2024-12-18  
**Quick Reference Version**: 1.0  
**For Detailed Info**: See TESTING_GUIDE.md and TEST_SCENARIOS.md
