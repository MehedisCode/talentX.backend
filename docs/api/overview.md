# API Overview

The TalentX REST API is built on **ASP.NET Core (.NET 10)** following RESTful design principles and JSON formatting standards.

---

## 🌐 Base URL & Routing

* **Base URL**: `http://localhost:5035/api/v1` (or configured `PORT` in `.env`)
* **Swagger UI**: `http://localhost:5035/swagger`
* **Health Endpoint**: `http://localhost:5035/health` or `http://localhost:5035/api/v1/health`

---

## 📋 API Endpoints Index

### 1. Employers API (`/api/v1/employers`)
| Method | Endpoint | Description | Auth Required |
| :--- | :--- | :--- | :--- |
| `POST` | `/api/v1/employers/register` | Register new employer account & complete profile | No |
| `POST` | `/api/v1/employers/verify-email` | Verify email with 6-digit OTP | No |
| `POST` | `/api/v1/employers/send-otp` | Send fresh 6-digit OTP verification code | No |
| `POST` | `/api/v1/employers/google` | Google OAuth sign-in / registration with ID token | No |

### 2. Authentication API (`/api/v1/auth`)
| Method | Endpoint | Description | Auth Required |
| :--- | :--- | :--- | :--- |
| `POST` | `/api/v1/auth/login` | Authenticate with email/password and obtain JWT | No |
| `POST` | `/api/v1/auth/refresh` | Refresh access token using refresh token | No |
| `POST` | `/api/v1/auth/logout` | Logout and invalidate refresh token | No |
| `GET` | `/api/v1/auth/user` | Get current authenticated user details | **Yes (`Bearer`)** |

### 3. Jobs API (`/api/v1/Jobs`)
| Method | Endpoint | Description | Auth Required |
| :--- | :--- | :--- | :--- |
| `POST` | `/api/v1/Jobs` | Create a job listing as a draft or published | **Yes (`Bearer`, Employer)** |
| `PATCH` | `/api/v1/Jobs/{id}` | Partially update / publish an owned job listing | **Yes (`Bearer`, Employer)** |

### 4. Diagnostics & Health (`/api/v1/health`)
| Method | Endpoint | Description | Auth Required |
| :--- | :--- | :--- | :--- |
| `GET` | `/health` / `/api/v1/health` | Server health status, UTC timestamp & uptime | No |

---

## 🔐 Authorization Header Format
For protected endpoints, include the JWT token in the `Authorization` request header:
```http
Authorization: Bearer <access_token_here>
```

---

## 📄 Standard Status Codes
* **`200 OK`**: Request succeeded with response payload.
* **`201 Created`**: Resource created successfully.
* **`400 Bad Request`**: Validation failure or malformed payload.
* **`401 Unauthorized`**: Authentication missing, invalid, or expired.
* **`403 Forbidden`**: Insufficient permissions.
* **`404 Not Found`**: Target entity not found.
* **`409 Conflict`**: Resource conflict (e.g., duplicate email address).
* **`500 Internal Server Error`**: Unexpected server-side error.
