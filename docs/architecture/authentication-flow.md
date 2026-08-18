# Authentication & User Lifecycle Architecture

This document details the authentication and identity model in TalentX, covering Employer Registration, Google OAuth, Email OTP verification, JWT sessions, and the JIT Talent lifecycle.

---

## 👥 User Roles & Lifecycle Models

The system defines distinct roles through `UserRole`:
- `Employer`: Registers via `/api/v1/employers`, verifies email via 6-digit OTP, creates and manages job postings.
- `Talent`: Job candidates. (Do **not** register via public sign-up; provisioned Just-In-Time on application).
- `Admin`: Platform administrators.

---

## 1. Employer Registration & OTP Flow

```
[ Frontend ]               [ Backend API / EmployersController ]          [ Infrastructure ]
     │                                     │                                      │
     │─── POST /api/v1/employers ─────────>│                                      │
     │    (Name, Email, Co, Pass)          │─── Create ApplicationUser (Identity)─>│
     │                                     │─── Add Employer Domain Entity ──────>│
     │                                     │─── Generate 6-Digit OTP ────────────>│ (OtpService)
     │                                     │─── Send OTP Email ──────────────────>│ (EmailService / SMTP)
     │<── 201 Created (EmployerDto) ───────│                                      │
     │                                     │                                      │
     │─── POST /api/v1/employers/verify-email ─>                                  │
     │    (Email, 6-digit OTP)             │─── Verify OTP in Memory ────────────>│
     │                                     │─── employer.MarkAsVerified() ────────>│ (Db update)
     │<── 200 OK ("Email verified") ───────│                                      │
```

### Key Logic & Edge Cases:
1. **Duplicate Email Prevention**:
   - If an employer with that email is already registered **and verified**, the system throws `ConflictException` (mapped to `409 Conflict`).
2. **Unverified Re-Registration**:
   - If a user registered previously but never completed OTP verification, submitting the form again updates their profile details and generates a fresh OTP code without failing.
3. **Secure OTP Generation**:
   - Cryptographically random 6-digit number via `RandomNumberGenerator.GetInt32(100000, 1000000)`.
   - Thread-safe storage with 10-minute automatic expiration.
   - Single-use invalidation (`TryRemove`) on verification prevents replay attacks.
   - Development bypass: `"000000"` magic code enabled only when `ASPNETCORE_ENVIRONMENT=Development`.

---

## 2. Google OAuth Sign-In Flow

Employers can authenticate via Google with zero manual password management.

```
[ Frontend (Google SDK) ]      [ EmployersController.GoogleSignIn ]       [ Infrastructure / Google API ]
         │                                      │                                      │
         │─── POST /api/v1/employers/google ───>│                                      │
         │    (idToken)                         │─── Verify Token Signature ──────────>│ (GoogleJsonWebSignature)
         │                                      │<── Claims (sub, email, name) ────────│
         │                                      │                                      │
         │                                      │─── FindOrCreateExternalUserAsync ───>│ (IdentityService)
         │                                      │─── Upsert Employer (IsVerified=true)─>│
         │<── 200 OK (EmployerDto) ─────────────│                                      │
```

---

## 3. JWT & Refresh Token Cookie Architecture

Authentication sessions use short-lived access tokens accompanied by long-lived refresh tokens stored securely in **HttpOnly, Secure, SameSite=None** cookies.

| Token | Storage | Lifetime | Purpose |
| :--- | :--- | :--- | :--- |
| **Access Token** (`TalentXAccessToken`) | HttpOnly Cookie | 15 Minutes | Authenticates API requests via standard JWT Bearer validation. |
| **Refresh Token** (`TalentXRefreshToken`) | HttpOnly Cookie + Database | 7 Days | Silent background rotation to issue fresh access tokens without re-prompting user login. |

### Refresh & Revocation Endpoints:
- `POST /api/v1/auth/login` ➔ Validates credentials, creates `RefreshToken` record in database, attaches cookies.
- `POST /api/v1/auth/refresh` ➔ Validates existing refresh token cookie, rotates it, and updates cookies.
- `POST /api/v1/auth/logout` ➔ Revokes refresh token in database and deletes cookies from the client response.

---

## 4. Candidate / Talent Just-In-Time (JIT) Provisioning

As specified in the TalentX Requirements:
- Candidates never register via a public `/signup` page.
- When an unregistered guest candidate applies to a job, the backend provisions a `User` record (`Role: Talent`, `IsVerified: false`) and dispatches a temporary password via email.
- Upon first login with the temporary password, the system forces a password reset redirect (`/change-password`), which activates the account (`IsVerified: true`).
