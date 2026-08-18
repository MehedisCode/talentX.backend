# API Specifications & Standards

The TalentX API is a RESTful web service built with ASP.NET Core (.NET 10). It utilizes standard HTTP methods, JSON payloads, and predictable HTTP status codes.

---

## 🌐 Base URL & Versioning

All API endpoints follow URL-based versioning:

```
http://localhost:5000/api/v1/[controller]
https://api.talentx.com/api/v1/[controller]
```

- **Local Development Port**: Dynamic port via `PORT` environment variable (defaults to `5000` or `5152` in `launchSettings.json`).
- **Interactive Swagger / OpenAPI UI**: `http://localhost:5000/swagger` (Enabled in Development).

---

## 📋 Available Controller Modules

| Controller | Route | Documentation | Description |
| :--- | :--- | :--- | :--- |
| **Employers** | `/api/v1/employers` | [employers.md](employers.md) | Employer registration, Google sign-in, OTP verification, resend OTP. |
| **Auth** | `/api/v1/auth` | [auth.md](auth.md) | Password authentication, token refreshing, session logout, current user profile. |
| **Health** | `/api/v1/health` | [health.md](health.md) | Service uptime, server datetime (UTC), diagnostics. |

---

## 🔒 Authentication & Headers

Protected endpoints require a valid JWT Access Token.
- In browser clients: Tokens are delivered automatically via **HttpOnly Secure Cookies** (`TalentXAccessToken`).
- In non-browser / external clients: Standard Authorization header can be supplied:
  ```http
  Authorization: Bearer <jwt-token>
  ```

---

## 📦 Standard HTTP Status Codes

| Code | Status | Meaning |
| :---: | :--- | :--- |
| `200` | **OK** | Request succeeded; response contains the requested resource. |
| `201` | **Created** | Resource created successfully (e.g. initial employer registration). |
| `400` | **Bad Request** | Validation failed, missing required fields, or invalid format. |
| `401` | **Unauthorized** | Missing, expired, or invalid authentication credentials. |
| `403` | **Forbidden** | Authenticated user lacks required role/permissions. |
| `404` | **Not Found** | The requested resource ID or email was not found. |
| `409` | **Conflict** | Resource already exists (e.g. duplicate verified employer email). |
| `500` | **Internal Server Error** | Unexpected server-side failure. |
