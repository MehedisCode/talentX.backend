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
| `GET` | `/api/v1/employers/me` | Retrieve authenticated employer profile details | **Yes (`Bearer`)** |
| `GET` | `/api/v1/employers/dashboard` | Employer dashboard metrics and recent jobs | **Yes (`Bearer`, Employer)** |
| `GET` | `/api/v1/employers/jobs` | Paginated listing of owned jobs | **Yes (`Bearer`, Employer)** |

### 2. Authentication API (`/api/v1/auth`)
| Method | Endpoint | Description | Auth Required |
| :--- | :--- | :--- | :--- |
| `POST` | `/api/v1/auth/login` | Authenticate with email/password and obtain JWT | No |
| `POST` | `/api/v1/auth/refresh` | Refresh access token using refresh token | No |
| `POST` | `/api/v1/auth/logout` | Logout and invalidate refresh token | No |
| `GET` | `/api/v1/auth/user` | Get current user details, password status, and auth providers | **Yes (`Bearer`)** |
| `POST` | `/api/v1/auth/change-password` | Change password (promotes Guest to Talent) | **Yes (`Bearer`)** |
| `POST` | `/api/v1/auth/add-password` | Set password for Google-only users & revoke other sessions | **Yes (`Bearer`)** |
| `POST` | `/api/v1/auth/forgot-password` | Request password reset token or Google sign-in prompt | No |
| `POST` | `/api/v1/auth/reset-password` | Reset password via token (promotes Guest to Talent) | No |
| `POST` | `/api/v1/auth/verify-email` | Verify candidate job application token & issue tokens | No |

### 3. Jobs API (`/api/v1/jobs`)
| Method | Endpoint | Description | Auth Required |
| :--- | :--- | :--- | :--- |
| `GET` | `/api/v1/jobs` | Public published jobs search & pagination | No |
| `GET` | `/api/v1/jobs/{id}` | Public job details by identifier | No |
| `POST` | `/api/v1/jobs` | Create a job listing (draft or published) | **Yes (`Bearer`, Employer)** |
| `PATCH` | `/api/v1/jobs/{id}` | Update an owned job listing | **Yes (`Bearer`, Employer)** |
| `GET` | `/api/v1/jobs/mine` | List all jobs owned by current employer | **Yes (`Bearer`, Employer)** |
| `GET` | `/api/v1/jobs/mine/{id}` | Retrieve single owned job by ID | **Yes (`Bearer`, Employer)** |
| `POST` | `/api/v1/jobs/generate-description` | Generate job description using AI | **Yes (`Bearer`, Employer)** |
| `POST` | `/api/v1/jobs/generate-skills` | Generate relevant job skills using AI | **Yes (`Bearer`, Employer)** |
| `POST` | `/api/v1/jobs/{id}/apply` | Candidate apply to job (multipart form with resume) | No |
| `PATCH` | `/api/v1/jobs/{id}/status` | Close a job listing | **Yes (`Bearer`, Employer)** |
| `PATCH` | `/api/v1/jobs/{id}/reopen` | Reopen a closed job listing | **Yes (`Bearer`, Employer)** |
| `DELETE` | `/api/v1/jobs/{id}` | Soft-delete a job listing | **Yes (`Bearer`, Employer)** |

### 4. Talents API (`/api/v1/talents`)
| Method | Endpoint | Description | Auth Required |
| :--- | :--- | :--- | :--- |
| `GET` | `/api/v1/talents/me` | Get authenticated talent profile | **Yes (`Bearer`, Talent)** |
| `PUT` | `/api/v1/talents/me` | Update talent profile fields | **Yes (`Bearer`, Talent)** |
| `GET` | `/api/v1/talents/me/application-prefill` | Pre-fill candidate job application form data | **Yes (`Bearer`, Talent)** |
| `POST` | `/api/v1/talents/me/resume/extract` | In-memory resume extraction via AI | **Yes (`Bearer`, Talent)** |
| `POST` | `/api/v1/talents/me/profile/apply-extraction` | Atomic save of extracted resume & profile fields | **Yes (`Bearer`, Talent)** |

### 5. Job Applications API (`/api/v1/job-applications`)
| Method | Endpoint | Description | Auth Required |
| :--- | :--- | :--- | :--- |
| `GET` | `/api/v1/job-applications/my-applications` | Get candidate's submitted job applications | **Yes (`Bearer`, Talent)** |
| `GET` | `/api/v1/job-applications/{id}` | View application details | **Yes (`Bearer`)** |
| `PATCH` | `/api/v1/job-applications/{id}/status` | Update candidate application status | **Yes (`Bearer`, Employer)** |

### 6. Resumes API (`/api/v1/resumes`)
| Method | Endpoint | Description | Auth Required |
| :--- | :--- | :--- | :--- |
| `GET` | `/api/v1/resumes/{storageKey}` | Stream resume file directly from storage | **Yes (`Bearer`)** |

### 7. Diagnostics & Health (`/api/v1/health`)
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
