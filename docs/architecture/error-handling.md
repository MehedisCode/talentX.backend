# Error Handling & Problem Details

TalentX uses a centralized exception handling architecture powered by **`GlobalExceptionMiddleware`** and custom domain/application exceptions. 

Errors are serialized into structured JSON responses following **RFC 7807** principles without exposing sensitive stack traces or internal infrastructure details.

---

## 🛠️ Global Exception Middleware

The middleware [`GlobalExceptionMiddleware`](file:///Users/jubair/Developer/asthait/talentx/talentx2026q3.backend/src/TalentX.Api/Middlewares/GlobalExceptionMiddleware.cs) catches all uncaught exceptions thrown during request execution and translates them to HTTP status codes.

```
Exception Thrown in Handler / Pipeline
                   │
                   ▼
       GlobalExceptionMiddleware
                   │
  ┌────────────────┼────────────────┬────────────────┬────────────────┐
  │                │                │                │                │
  ▼                ▼                ▼                ▼                ▼
FluentValidation  ConflictException NotFoundException Unauthorized    Unhandled
  (400 Bad Req)    (409 Conflict)   (404 Not Found)  (401 Unauth)    (500 Error)
```

---

## 📊 Exception to HTTP Status Code Mapping

| Exception Type | HTTP Status | Response Structure | Example Scenario |
| :--- | :---: | :--- | :--- |
| `FluentValidation.ValidationException` | `400 Bad Request` | `{"message": "Validation Failed", "errors": ["Email is required"]}` | Invalid email format, weak password |
| `Application.Common.Exceptions.ValidationException` | `400 Bad Request` | `{"message": "<Custom error message>"}` | Invalid or expired OTP code |
| `Application.Common.Exceptions.UnauthorizedException` | `401 Unauthorized` | `{"message": "<Unauthorized message>"}` | Invalid Google OAuth token, bad credentials |
| `Application.Common.Exceptions.ForbiddenException` | `403 Forbidden` | `{"message": "<Forbidden message>"}` | Accessing employer resources as candidate |
| `Application.Common.Exceptions.NotFoundException` | `404 Not Found` | `{"message": "<Not found message>"}` | Employer / Job ID does not exist |
| `Application.Common.Exceptions.ConflictException` | `409 Conflict` | `{"message": "<Conflict details>"}` | Employer email already registered & verified |
| `System.Exception` (General) | `500 Internal Server Error` | `{"message": "<Sanitized error>"}` | Database timeout, unhandled runtime error |

---

## 🔍 Standard Error Payloads

### 1. Validation Error Response (400 Bad Request)
```json
{
  "message": "Validation Failed",
  "errors": [
    "Email must not exceed 255 characters.",
    "Password must contain at least one uppercase letter."
  ]
}
```

### 2. Conflict Error Response (409 Conflict)
```json
{
  "message": "An employer with email 'hr@acme.com' is already registered and verified. Please sign in."
}
```

### 3. Not Found Error Response (404 Not Found)
```json
{
  "message": "Employer with email 'unknown@acme.com' not found."
}
```
