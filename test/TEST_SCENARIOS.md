# Authentication Test Scenarios & Patterns

This document details the specific test scenarios and business logic patterns for authentication testing.

## Login Flow Test Scenarios

### Happy Path Scenario
```
User enters email: user@example.com
User enters password: ValidPassword123!
   ↓
IdentityService validates credentials
   ↓
TokenService generates access & refresh tokens
   ↓
Response: Success with tokens and user info
```

**Test Coverage**:
```csharp
[Fact]
public async Task Handle_WithValidCredentials_ShouldReturnSuccessfulLoginResult()
{
    // Email is valid
    // Password matches stored hash
    // User is not locked out
    // Tokens are generated successfully
    // ✓ User can login
}
```

### Invalid Credentials Scenario
```
User enters email: testuser@example.com
User enters password: WrongPassword123!
   ↓
IdentityService finds user but password fails
   ↓
Access failed count increments
   ↓
Response: Failure with "Invalid credentials" error
```

**Test Coverage**:
```csharp
[Fact]
public async Task Handle_WithInvalidPassword_ShouldReturnFailureResult()
{
    // Email exists
    // Password doesn't match
    // Access failed count incremented
    // User not locked (yet)
    // ✓ Invalid credentials error returned
    // ✓ Token service not called
}
```

### Account Lockout Scenario
```
User attempts login with wrong password 5 times
   ↓
Account becomes locked
   ↓
Next login attempt (even with correct password)
   ↓
Response: Failure with "Account is locked out" error
```

**Test Coverage**:
```csharp
[Fact]
public async Task Handle_WithLockedOutAccount_ShouldReturnFailureResult()
{
    // User exists
    // Account is locked (IsLockedOut = true)
    // ✓ Error returned before password check
    // ✓ Token service not called
}
```

### Non-existent User Scenario
```
User enters email: nonexistent@example.com
User enters password: SomePassword123!
   ↓
IdentityService finds no user with that email
   ↓
Response: Failure with "Invalid credentials" error (don't reveal user doesn't exist)
```

**Test Coverage**:
```csharp
[Fact]
public async Task Handle_WithNonExistentUser_ShouldReturnFailureResult()
{
    // Email doesn't exist in database
    // ✓ Same error as wrong password (security)
    // ✓ No user info leaked
}
```

### Token Generation Failure Scenario
```
User credentials valid ✓
   ↓
TokenService fails to create tokens (DB issue, JWT error)
   ↓
Response: Failure with token generation error
```

**Test Coverage**:
```csharp
[Fact]
public async Task Handle_WhenTokenCreationFails_ShouldReturnFailureResult()
{
    // Credentials validated successfully
    // Token creation fails
    // ✓ Login fails
    // ✓ Error propagated to client
}
```

## Refresh Token Flow Test Scenarios

### Valid Token Refresh Scenario
```
User has valid refresh token: old-refresh-token-xyz
Client sends: { "refreshToken": "old-refresh-token-xyz" }
   ↓
TokenService validates token
   ↓
Old token marked as revoked
New token pair generated
   ↓
Response: Success with new tokens
```

**Test Coverage**:
```csharp
[Fact]
public async Task Handle_WithValidRefreshToken_ShouldReturnNewTokens()
{
    // Token exists in database
    // Token is not expired
    // Token hasn't been revoked
    // ✓ New tokens generated
    // ✓ Old token marked revoked
    // ✓ Rotation successful
}
```

### Expired Token Scenario
```
User has expired refresh token
   ↓
TokenService checks expiration
   ↓
ExpiresAt < DateTime.UtcNow
   ↓
Response: Failure with "Token has expired" error
```

**Test Coverage**:
```csharp
[Fact]
public async Task Handle_WithExpiredRefreshToken_ShouldReturnFailureResult()
{
    // Token stored in database
    // ExpiresAt is in the past
    // ✓ Error returned
    // ✓ No new tokens generated
}
```

### Token Reuse Detection Scenario (Security Critical)
```
User attempts to use same refresh token twice
First request: Token validated and rotated ✓
   ↓
Second request: Same token again
   ↓
TokenService detects: Token was already revoked (reuse detected!)
   ↓
SECURITY: Revoke all tokens for this user (assume compromised session)
Response: Failure with "Reuse detected" error + all sessions revoked
```

**Test Coverage**:
```csharp
[Fact]
public async Task Handle_WithReuseDetectedToken_ShouldReturnFailureResult()
{
    // Token has been revoked already
    // IsActive = false
    // ✓ Reuse detected
    // ✓ All user sessions revoked
    // ✓ User must login again
    // ✓ Security threat mitigated
}
```

### Invalid Token Format Scenario
```
Client sends malformed refresh token
   ↓
TokenService hashes and searches database
   ↓
No matching hash found
   ↓
Response: Failure with "Invalid refresh token" error
```

**Test Coverage**:
```csharp
[Fact]
public async Task Handle_WithInvalidRefreshToken_ShouldReturnFailureResult()
{
    // Token doesn't exist in database
    // Hash doesn't match any stored token
    // ✓ Error returned
}
```

### User Deleted After Token Issued Scenario
```
User had valid token: token-abc
User account deleted
   ↓
User attempts token refresh
   ↓
TokenService finds token but user no longer exists
   ↓
Response: Failure with "User not found" error
```

**Test Coverage**:
```csharp
[Fact]
public async Task Handle_WhenUserNotFound_ShouldReturnFailureResult()
{
    // Token is valid and active
    // User associated with token was deleted
    // ✓ Error returned
    // ✓ Graceful handling of edge case
}
```

## Logout Flow Test Scenarios

### Successful Logout Scenario
```
User sends logout request with refresh token: token-xyz
   ↓
TokenService finds token in database
   ↓
Token marked as revoked (RevokedAt = now)
   ↓
Response: Success (200 OK)
```

**Test Coverage**:
```csharp
[Fact]
public async Task Handle_WithValidRefreshToken_ShouldRevokeTokenSuccessfully()
{
    // Token exists
    // Token is active
    // ✓ Token revoked
    // ✓ RevokedAt timestamp set
    // ✓ Success response
}
```

### Already Revoked Token Logout Scenario
```
User attempts logout with already-revoked token
   ↓
TokenService finds token but RevokedAt is set
   ↓
Response: Failure with "Token already revoked" error
```

**Test Coverage**:
```csharp
[Fact]
public async Task Handle_WithAlreadyRevokedToken_ShouldReturnFailureResult()
{
    // Token exists in database
    // RevokedAt is already set
    // ✓ Error returned
    // ✓ Idempotence not guaranteed (by design)
}
```

### Invalid Token Logout Scenario
```
User sends logout with invalid/fake token
   ↓
TokenService searches database, finds nothing
   ↓
Response: Failure with "Invalid token" error
```

**Test Coverage**:
```csharp
[Fact]
public async Task Handle_WithInvalidRefreshToken_ShouldReturnFailureResult()
{
    // Token not in database
    // ✓ Error returned gracefully
}
```

### Database Error During Revocation
```
User submits valid logout request
   ↓
Token found and ready to revoke
   ↓
Database save fails (connection error, constraint violation, etc.)
   ↓
Response: Failure with database error message
```

**Test Coverage**:
```csharp
[Fact]
public async Task Handle_WithDatabaseError_ShouldReturnFailureResult()
{
    // Token exists
    // Database update fails
    // ✓ Error propagated
    // ✓ Exception handled gracefully
}
```

## Password Validation Test Scenarios

### Email Validation Rules
```
✓ Valid Emails:
  - simple@example.com
  - user.name@example.co.uk
  - user+tag@example.com
  - test_123@test-domain.com

✗ Invalid Emails:
  - invalid-email (no @)
  - @example.com (no local part)
  - user@ (no domain)
  - user@.com (no domain name)
  - user name@example.com (spaces)
```

**Test Coverage**:
```csharp
[Theory]
[InlineData("valid.email@example.com", true)]
[InlineData("invalid-email", false)]
[InlineData("@example.com", false)]
public void Validate_EmailFormat(string email, bool shouldBeValid)
{
    // Tests RFC 5322 email validation
    // ✓ Valid emails accepted
    // ✓ Invalid emails rejected
}
```

### Email Length Validation
```
Minimum: 1 character (a@b.c)
Maximum: 256 characters
```

**Test Coverage**:
```csharp
[Fact]
public void Validate_WithEmailAtMaxLength_ShouldSucceed()
{
    var email = new string('a', 250) + "@test.com";  // 256 chars
    // ✓ Accepted
}

[Fact]
public void Validate_WithEmailExceedingMaxLength_ShouldFail()
{
    var email = new string('a', 250) + "@test.com.extra";  // 257+ chars
    // ✓ Rejected
}
```

### Password Requirements
Per ASP.NET Core Identity defaults:
- Minimum length: 8 characters
- Must contain 1 uppercase letter (A-Z)
- Must contain 1 lowercase letter (a-z)
- Must contain 1 digit (0-9)
- Must contain 1 special character (!@#$%^&*)

**Validation Logic**:
```csharp
RuleFor(x => x.Password)
    .NotEmpty().WithMessage("Password is required.")
    .MaximumLength(256).WithMessage("Password must not exceed 256 characters.");
    
// Note: Actual character requirements enforced by UserManager.CreateAsync()
```

**Test Coverage**:
```csharp
[Fact]
public void Validate_WithValidPassword_ShouldSucceed()
{
    var password = "ValidPassword123!";
    // ✓ Accepted by validator
    // Note: Full complexity check by UserManager during creation
}

[Fact]
public void Validate_WithEmptyPassword_ShouldFail()
{
    var password = "";
    // ✓ Rejected
}
```

## User Identity Service Test Scenarios

### Create User with Valid Role
```
Input: email, password, role
   ↓
1. Check if role exists
2. Check if email already exists
3. Create user with username = email
4. Hash and store password
5. Assign user to role
   ↓
Output: Success with userId
```

**Test Coverage**:
```csharp
[Fact]
public async Task CreateApplicationUserAsync_WithValidParameters_ShouldCreateUserSuccessfully()
{
    // All preconditions met
    // ✓ User created
    // ✓ Password hashed
    // ✓ Role assigned
    // ✓ UserId returned
}
```

### Duplicate Email Prevention
```
Input: email that already exists
   ↓
Check if email exists in database
   ↓
Found: Return error "Email is already in use"
```

**Test Coverage**:
```csharp
[Fact]
public async Task CreateApplicationUserAsync_WithDuplicateEmail_ShouldReturnFailure()
{
    // Email already exists
    // ✓ Error returned
    // ✓ No user created
    // ✓ Database not modified
}
```

### Create User Failure Rollback
```
Input: valid email, password, role
   ↓
1. Role check: ✓ Pass
2. Email duplicate check: ✓ Pass
3. Create user: ✓ Success
4. Assign role: ✗ FAIL
   ↓
Rollback: Delete created user
   ↓
Output: Failure with error
```

**Test Coverage**:
```csharp
[Fact]
public async Task CreateApplicationUserAsync_WhenRoleAssignmentFails_ShouldDeleteUserAndReturnFailure()
{
    // User creation succeeds
    // Role assignment fails
    // ✓ User is deleted (rollback)
    // ✓ Error returned
    // ✓ Database in clean state
}
```

### Validate Password with Access Failed Tracking
```
Attempt 1: Wrong password
   ↓
AccessFailedCount: 0 → 1
   ↓
Attempt 2: Wrong password
   ↓
AccessFailedCount: 1 → 2
   ↓
Attempt 5: Wrong password
   ↓
AccessFailedCount: 4 → 5 (or locked, depending on config)
   ↓
Account locked if count reaches threshold
```

**Test Coverage**:
```csharp
[Fact]
public async Task ValidatePasswordAsync_WithIncorrectPassword_ShouldIncrementAccessFailedCount()
{
    // Correct email, wrong password
    // ✓ User found
    // ✓ Not locked out
    // ✓ Password check fails
    // ✓ AccessFailedAsync called
    // ✓ Access failed count incremented
}

[Fact]
public async Task ValidatePasswordAsync_WithCorrectPassword_ShouldResetAccessFailedCount()
{
    // Correct email and password
    // ✓ User found
    // ✓ Not locked out
    // ✓ Password check succeeds
    // ✓ ResetAccessFailedCountAsync called
    // ✓ Count reset to 0
}
```

### Locked Out Account Prevention
```
User enters password (correct or not)
   ↓
Check: IsLockedOut?
   ↓
Yes: Return error "Account is locked out"
     (Don't check password, don't increment count)
   ↓
No: Proceed with password validation
```

**Test Coverage**:
```csharp
[Fact]
public async Task ValidatePasswordAsync_WithLockedOutAccount_ShouldReturnFailure()
{
    // User exists
    // IsLockedOut = true
    // ✓ Error returned immediately
    // ✓ Password not checked
    // ✓ Access failed count not modified
}
```

## Error Scenario Combinations

### Multiple Errors from Service
When a service returns multiple error messages, test that all are propagated:

```csharp
[Fact]
public async Task Handle_WithMultipleErrorsFromService_ShouldReturnAllErrors()
{
    var errors = new[] { "Error 1", "Error 2", "Error 3" };
    _tokenServiceMock
        .Setup(x => x.CreateAsync(It.IsAny<string>()))
        .ReturnsAsync(IdentityResponse<TokenResponse>.Fail(errors));

    // ✓ All three errors returned to client
}
```

## Edge Cases & Boundary Values

### Very Long Email
```
Email: a@b.c (5 characters)
Email: verylongemailaddress@verylongdomainname.extension (250 characters)
Email: maximum.length.email.with.many.characters.repeated@verylongdomainname.extension.com (exactly 256)

✓ All valid emails should be processed
```

### Empty/Null Values
```
Email: "" (empty string) → ✗ Reject
Email: null → ✗ Reject
Password: "" (empty string) → ✗ Reject
Password: null → ✗ Reject
RefreshToken: "" (empty string) → ✗ Reject
RefreshToken: null → ✗ Reject
```

### Whitespace Handling
```
Email: "  user@example.com  " (with spaces)
Email: "user name@example.com" (space in middle)
Password: "   " (only whitespace) → Technically passes empty check, but fails complexity check

Note: Validator doesn't trim by default
```

## Performance Considerations in Tests

### Mock Timing
Tests should complete quickly. If a test takes > 1 second:
- Check for actual async delays (avoid using Task.Delay in tests)
- Verify no database round-trips in mocked tests
- Ensure mocks don't introduce latency

### Cancellation Token Testing
```csharp
[Fact]
public async Task Handle_ShouldRespectCancellationToken()
{
    var cts = new CancellationTokenSource();
    cts.Cancel();
    
    // Test should complete, not hang
    _ = await _handler.Handle(command, cts.Token);
    
    // ✓ No timeout
    // ✓ Clean cancellation
}
```

## Security Testing Patterns

### No Information Leakage
```csharp
// ✓ Correct: Same error for wrong password and non-existent user
public async Task Handle_WithInvalidCredentials_ShouldNotRevealUserStatus()
{
    // Attempt 1: Wrong password for existing user → "Invalid credentials"
    // Attempt 2: Email doesn't exist → "Invalid credentials" (SAME)
    
    // ✓ Attacker can't enumerate valid emails
}
```

### Token Rotation Security
```csharp
// ✓ Correct: Old token marked revoked, new token issued
public async Task Handle_ShouldRotateTokenOnRefresh()
{
    // Old token: active
    // After refresh: RevokedAt = now, IsActive = false
    // New token: active, issued
    
    // ✓ Reuse of old token detected on next attempt
}
```

### Reuse Detection & Revocation
```csharp
// ✓ Correct: Aggressive session revocation on reuse detection
public async Task Handle_WithReuseDetectedToken_ShouldRevokeAllSessions()
{
    // Same token used twice
    // Second attempt: All tokens for user revoked
    // User must re-authenticate
    
    // ✓ Suspected account compromise handled
}
```

---

## Test Execution Workflow

1. **Developer writes code** → Code changes auth logic
2. **Developer runs tests** → `dotnet test` command
3. **Tests verify behavior**:
   - Correct behavior: ✓ Green
   - Broken behavior: ✗ Red
4. **Developer fixes code** based on test feedback
5. **CI/CD pipeline** runs tests automatically on every commit

## Summary of Coverage

| Component | Happy Path | Error Cases | Edge Cases | Total Tests |
|-----------|-----------|------------|-----------|------------|
| Login Validator | 5 | 8 | 3 | 16 |
| Login Handler | 3 | 7 | 1 | 11 |
| Refresh Handler | 2 | 6 | 1 | 9 |
| Logout Handler | 3 | 6 | 1 | 10 |
| IdentityService | 8 | 15 | 3 | 26 |
| **TOTAL** | **21** | **42** | **9** | **72** |

---

**Last Updated**: 2024-12-18  
**Focus Areas**: Authentication, Authorization, Token Management, Security  
**Framework**: ASP.NET Core Identity, JWT
