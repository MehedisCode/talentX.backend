# Error Handling & Problem Details

The TalentX Backend implements a standardized, centralized exception handling strategy via custom middleware and a clean application exception hierarchy.

---

## 🏛️ Exception Handling Architecture

All exceptions thrown during request execution are intercepted by [`ExceptionMiddleware`](file:///Users/jubair/Developer/asthait/talentx/talentx2026q3.backend/src/TalentX.Infrastructure/Middlewares/ExceptionMiddleware.cs) in the Infrastructure layer before reaching the client:

```
[ HTTP Request ]
       │
       ▼
[ ExceptionMiddleware ]
       │
       ├─► [ Next Middleware / Controller / MediatR Handler ]
       │          │
       │          └── Throws Exception
       │                     │
       ◄─────────────────────┘
       ▼
[ HandleExceptionAsync ]
       │
       ├── FluentValidation.ValidationException ──► HTTP 400 (Bad Request + Validation Errors)
       ├── AppException (ConflictException)      ──► HTTP 409 (Conflict)
       ├── AppException (NotFoundException)      ──► HTTP 404 (Not Found)
       ├── AppException (UnauthorizedException)  ──► HTTP 401 (Unauthorized)
       ├── AppException (ForbiddenException)     ──► HTTP 403 (Forbidden)
       ├── AppException (BadRequestException)    ──► HTTP 400 (Bad Request)
       └── Unhandled Exception                   ──► HTTP 500 (Internal Server Error)
```

---

## 🧱 Application Exception Hierarchy

Located under `src/TalentX.Application/Common/Exceptions/`:

```
AppException (abstract, base class with HttpStatusCode)
├── BadRequestException    (HTTP 400)
├── ValidationException    (HTTP 400)
├── UnauthorizedException  (HTTP 401)
├── ForbiddenException     (HTTP 403)
├── NotFoundException      (HTTP 404)
└── ConflictException      (HTTP 409)
```

### Example: Throwing Domain/Business Exceptions in Handlers

```csharp
// Duplicate email conflict
var existingUser = await _unitOfWork.Users.GetByEmailAsync(request.Email);
if (existingUser != null)
{
    throw new ConflictException("An account with this email address already exists.");
}

// Resource not found
var employer = await _unitOfWork.Employers.GetByIdAsync(request.EmployerId);
if (employer == null)
{
    throw new NotFoundException("Employer not found.");
}
```

---

## 📋 Response Payloads

### 1. Validation Error (HTTP 400 Bad Request)
```json
{
  "message": "Validation Failed",
  "errors": [
    "Email address is required.",
    "Password must be at least 8 characters long and contain uppercase, lowercase, digit, and special character."
  ]
}
```

### 2. Conflict Error (HTTP 409 Conflict)
```json
{
  "message": "An account with this email address already exists."
}
```

### 3. Unauthorized Error (HTTP 401 Unauthorized)
```json
{
  "message": "Invalid credentials."
}
```

### 4. Internal Server Error (HTTP 500)
In production, internal error details and stack traces are suppressed to avoid security disclosures:
```json
{
  "message": "An unexpected error occurred."
}
```
