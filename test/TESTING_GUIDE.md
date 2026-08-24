# Authentication Testing Guide

This document provides a comprehensive guide to understanding and working with the authentication unit tests in the TalentX backend project.

## Overview

The authentication test suite covers all critical authentication flows using unit tests with xUnit and Moq for mocking external dependencies. The tests are organized into two main projects:

- **TalentX.Application.Tests**: Tests for CQRS handlers and validators
- **TalentX.Infrastructure.Tests**: Tests for identity and token services

## Test Projects Structure

```
test/
├── TalentX.Application.Tests/
│   └── Features/
│       └── Auth/
│           ├── Login/
│           │   ├── LoginCommandHandlerTests.cs
│           │   └── LoginCommandValidatorTests.cs
│           ├── RefreshToken/
│           │   └── RefreshTokenCommandHandlerTests.cs
│           └── Logout/
│               └── LogoutCommandHandlerTests.cs
└── TalentX.Infrastructure.Tests/
    └── Identity/
        └── Services/
            └── IdentityServiceTests.cs
```

## Test Dependencies

### NuGet Packages
- **xunit** (2.4.2): Test framework
- **xunit.runner.visualstudio** (2.5.4): VS integration
- **Microsoft.NET.Test.Sdk** (17.9.0): Test SDK
- **Moq** (4.20.70): Mocking library
- **FluentAssertions** (6.12.0): Fluent assertion library

## Running the Tests

### Using Visual Studio
1. Open Test Explorer: `Test > Test Explorer` or `Ctrl+E, T`
2. Click "Run All Tests" or select specific tests to run
3. View results in the Test Explorer window

### Using .NET CLI

```bash
# Run all tests
dotnet test

# Run tests in specific project
dotnet test test/TalentX.Application.Tests/TalentX.Application.Tests.csproj

# Run with verbose output
dotnet test --verbosity normal

# Run specific test class
dotnet test --filter "ClassName=LoginCommandHandlerTests"

# Run with code coverage
dotnet test /p:CollectCoverage=true
```

## Testing Patterns & Best Practices

### AAA (Arrange-Act-Assert) Pattern

All tests follow the AAA pattern:

```csharp
[Fact]
public async Task Handle_WithValidCredentials_ShouldReturnSuccessfulLoginResult()
{
    // ARRANGE: Set up test data and mocks
    var email = "testuser@example.com";
    var password = "ValidPassword123!";
    var userId = "user-123";
    
    var command = new LoginCommand(email, password);
    
    _identityServiceMock
        .Setup(x => x.ValidatePasswordAsync(email, password))
        .ReturnsAsync(IdentityResponse<PasswordValidationResult>.Ok(...));

    // ACT: Execute the method being tested
    var result = await _handler.Handle(command, CancellationToken.None);

    // ASSERT: Verify the results
    result.Success.Should().BeTrue();
    result.Data!.UserId.Should().Be(userId);
}
```

### Mocking Strategy

The tests use Moq to mock external dependencies:

1. **Service Mocks**: Interface implementations are mocked (IIdentityService, ITokenService)
2. **Identity Framework Mocks**: UserManager and RoleManager are mocked
3. **Controlled Behavior**: Setup returns expected values for test scenarios

### Fluent Assertions

Tests use FluentAssertions for readable assertions:

```csharp
// Instead of Assert.IsTrue(result.Success)
result.Success.Should().BeTrue();

// Instead of Assert.Equal(email, result.Data.Email)
result.Data!.Email.Should().Be(email);

// Verify method calls
_identityServiceMock.Verify(x => x.ValidatePasswordAsync(email, password), Times.Once);
```

## Test Coverage Areas

### 1. LoginCommandValidator Tests

**File**: `TalentX.Application.Tests/Features/Auth/Login/LoginCommandValidatorTests.cs`

**Coverage**:
- ✅ Email validation (format, length, emptiness)
- ✅ Password validation (emptiness, length)
- ✅ Combined field validation
- ✅ Boundary value analysis
- ✅ Edge cases (whitespace, special characters)

**Example Test Case**:
```csharp
[Theory]
[InlineData("valid.email@example.com")]
[InlineData("user@domain.co.uk")]
public void Validate_WithValidEmail_ShouldSucceed(string email)
{
    // Arrange
    var command = new LoginCommand(email, "ValidPassword123!");

    // Act
    var result = _validator.Validate(command);

    // Assert
    result.IsValid.Should().BeTrue();
}
```

### 2. LoginCommandHandler Tests

**File**: `TalentX.Application.Tests/Features/Auth/Login/LoginCommandHandlerTests.cs`

**Coverage**:
- ✅ Happy path (successful login)
- ✅ Invalid credentials
- ✅ Non-existent user
- ✅ Locked-out account
- ✅ Token generation failures
- ✅ Data integrity verification
- ✅ Cancellation token handling

**Scenario Groups**:
- Happy Path: Successful authentication flow
- Error Conditions: Various failure scenarios
- Data Integrity: Email and UserId preservation
- Async/Cancellation: Token and async behavior

### 3. RefreshTokenCommandHandler Tests

**File**: `TalentX.Application.Tests/Features/Auth/RefreshToken/RefreshTokenCommandHandlerTests.cs`

**Coverage**:
- ✅ Token refresh with valid token
- ✅ Invalid refresh token
- ✅ Expired token
- ✅ Token reuse detection
- ✅ User not found
- ✅ Token rotation correctness

**Key Scenarios**:
- Valid token → new token pair generation
- Invalid token → error response
- Reused token → revocation and error
- Token format validation

### 4. LogoutCommandHandler Tests

**File**: `TalentX.Application.Tests/Features/Auth/Logout/LogoutCommandHandlerTests.cs`

**Coverage**:
- ✅ Successful token revocation
- ✅ Invalid token revocation
- ✅ Already revoked token
- ✅ User not found
- ✅ Database errors
- ✅ Idempotence verification

**Scenarios**:
- Happy path: Token successfully revoked
- Error handling: Various failure scenarios
- Boundary values: Different token formats
- Idempotence: Multiple logout calls

### 5. IdentityService Tests

**File**: `TalentX.Infrastructure.Tests/Identity/Services/IdentityServiceTests.cs`

**Coverage**:
- ✅ User creation with role assignment
- ✅ Password validation
- ✅ Access failed count management
- ✅ Account lockout detection
- ✅ Email retrieval
- ✅ Email update
- ✅ User deletion
- ✅ Multiple error handling

**Test Groups**:

#### CreateApplicationUserAsync
- Valid user creation
- Invalid role
- Duplicate email
- User creation failure
- Role assignment failure with cleanup

#### ValidatePasswordAsync
- Valid credentials
- Non-existent user
- Locked-out account
- Incorrect password
- Access failed count increment
- Access failed count reset

#### GetApplicationUserEmailByUserIdAsync
- Valid user ID
- Non-existent user

#### UpdateApplicationUserEmailAsync
- Successful email update
- Non-existent user
- Email change failure
- Username update failure

#### DeleteApplicationUserAsync
- Successful deletion
- Non-existent user
- Deletion failure

## Test Data & Fixtures

### Common Test Data

```csharp
// Email Examples
- valid.email@example.com
- user@domain.co.uk
- test123@test-domain.com
- Invalid: invalid-email, @example.com

// Password Examples
- ValidPassword123!
- AnotherValid@Pass99
- TestPassword#2024

// User Roles
- UserRole.User
- UserRole.Admin

// Token Examples
- Refresh: valid.refresh.token, expired.refresh.token
- Access: access.token.value
```

### Mock Setup Patterns

```csharp
// Identity Service Mock
_identityServiceMock
    .Setup(x => x.ValidatePasswordAsync(email, password))
    .ReturnsAsync(IdentityResponse<PasswordValidationResult>.Ok(data));

// Token Service Mock
_tokenServiceMock
    .Setup(x => x.CreateAsync(userId))
    .ReturnsAsync(IdentityResponse<TokenResponse>.Ok(tokenResponse));

// User Manager Mock
_userManagerMock
    .Setup(x => x.FindByEmailAsync(email))
    .ReturnsAsync(user);
```

## Common Testing Pitfalls & Solutions

### 1. **Mock Not Setup Correctly**
**Problem**: Test fails because mock method isn't configured
```csharp
// ❌ Wrong: Missing setup
_identityServiceMock.Setup(x => x.ValidatePasswordAsync(email, password));

// ✅ Correct: Include return value
_identityServiceMock
    .Setup(x => x.ValidatePasswordAsync(email, password))
    .ReturnsAsync(IdentityResponse<PasswordValidationResult>.Ok(data));
```

### 2. **Async/Await Issues**
**Problem**: Test doesn't wait for async operations
```csharp
// ❌ Wrong: Missing await
var result = _handler.Handle(command, CancellationToken.None);

// ✅ Correct: Await the task
var result = await _handler.Handle(command, CancellationToken.None);
```

### 3. **Verify Wrong Times**
**Problem**: Incorrect Times parameter in Verify
```csharp
// ❌ Wrong: Expects two calls
_mock.Verify(x => x.Method(), Times.Exactly(2));

// ✅ Correct: Expects one call
_mock.Verify(x => x.Method(), Times.Once);
```

### 4. **Null Reference in Test**
**Problem**: Not initializing mock or handler
```csharp
// ❌ Wrong: _identityServiceMock is null
_handler = new LoginCommandHandler(null, _tokenServiceMock.Object);

// ✅ Correct: Initialize properly
_handler = new LoginCommandHandler(_identityServiceMock.Object, _tokenServiceMock.Object);
```

## Adding New Tests

### Step 1: Identify Test Scenarios
- Happy path (success case)
- Error cases (various failures)
- Boundary values (min/max inputs)
- Edge cases (unusual but valid inputs)

### Step 2: Create Test Class
```csharp
public class NewFeatureHandlerTests
{
    private readonly Mock<IService> _serviceMock;
    private readonly Handler _handler;

    public NewFeatureHandlerTests()
    {
        _serviceMock = new Mock<IService>();
        _handler = new Handler(_serviceMock.Object);
    }
}
```

### Step 3: Write Test Method
```csharp
[Fact]
public async Task Handle_WithValidInput_ShouldReturnSuccess()
{
    // Arrange
    var input = CreateTestInput();
    _serviceMock.Setup(x => x.Method()).ReturnsAsync(expectedResult);

    // Act
    var result = await _handler.Handle(input, CancellationToken.None);

    // Assert
    result.Success.Should().BeTrue();
    _serviceMock.Verify(x => x.Method(), Times.Once);
}
```

## Troubleshooting Guide

### Tests Not Discovered
- Verify test class is public
- Test methods must be public and return Task (for async)
- Class name should end with "Tests"

### Tests Fail with "Object Reference Not Set"
- Check mock setup before using
- Ensure all dependencies are initialized in constructor

### Intermittent Test Failures
- Check for race conditions in async code
- Verify cancellation token usage
- Review mock setup for proper returns

### Mock.Verify Fails
- Confirm method is actually being called
- Check parameter values match exactly
- Use It.IsAny<>() if parameter variation is expected

## Code Coverage

To measure code coverage:

```bash
# Generate coverage report
dotnet test /p:CollectCoverage=true /p:CoverageFormat=opencover

# View coverage results
# Coverage reports are generated in coverage/ directory
```

**Target Coverage Goals**:
- Login flow: > 95%
- Refresh token: > 95%
- Logout: > 95%
- IdentityService: > 90%

## Integration with CI/CD

Tests run automatically in CI/CD pipeline:

```yaml
# Example GitHub Actions
- name: Run Tests
  run: dotnet test --verbosity normal

- name: Check Coverage
  run: dotnet test /p:CollectCoverage=true --minimum-coverage-percentage 80
```

## Resources & References

- [xUnit.net Documentation](https://xunit.net/docs/getting-started/netfx)
- [Moq Documentation](https://github.com/moq/moq4/wiki/Quickstart)
- [FluentAssertions Guide](https://fluentassertions.com/)
- [Unit Testing Best Practices](https://docs.microsoft.com/en-us/dotnet/core/testing/unit-testing-best-practices)

## Best Practices Summary

1. ✅ **Use AAA Pattern**: Arrange-Act-Assert structure
2. ✅ **One Assertion Focus**: Each test should verify one behavior
3. ✅ **Descriptive Names**: Test name should describe what is being tested
4. ✅ **Mock External Dependencies**: Only test the handler, not services
5. ✅ **Use Fixtures**: Reuse common test data
6. ✅ **Verify Interactions**: Use Mock.Verify to ensure dependencies are called correctly
7. ✅ **Test Edge Cases**: Include boundary values and error scenarios
8. ✅ **Keep Tests Independent**: Each test should be able to run in any order
9. ✅ **Use Theory for Variations**: [Theory] with [InlineData] for multiple inputs
10. ✅ **Async/Await Properly**: Always await async methods

---

**Last Updated**: 2024-12-18  
**Framework Version**: .NET 10.0  
**Test Framework**: xUnit 2.4.2
