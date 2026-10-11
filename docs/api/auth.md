# Authentication API

The Authentication API provides endpoints for user login, token refresh, session logout, and retrieving current user profile information.

---

## 1. User Login

Authenticates user credentials and returns JWT access and refresh tokens.

* **Method**: `POST`
* **Route**: `/api/v1/auth/login`
* **Auth**: Public (`[AllowAnonymous]`)

### Request Body
```json
{
  "email": "employer@talentx.com",
  "password": "Password123!"
}
```

### Response (`200 OK`)
```json
{
  "userId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "email": "employer@talentx.com",
  "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6...",
  "refreshToken": "4a71b2d0..."
}
```

### Error Responses
* `400 Bad Request`: Validation failure.
* `401 Unauthorized`: Invalid credentials.

---

## 2. Refresh Token

Issues a new access token and rotated refresh token using an unexpired refresh token.

* **Method**: `POST`
* **Route**: `/api/v1/auth/refresh`
* **Auth**: Public (`[AllowAnonymous]`)

### Request Body
```json
{
  "refreshToken": "4a71b2d0..."
}
```

### Response (`200 OK`)
```json
{
  "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6...",
  "refreshToken": "8f33c1a9..."
}
```

---

## 3. Current User Profile

Returns profile and role claims for the currently authenticated caller.

* **Method**: `GET`
* **Route**: `/api/v1/auth/user`
* **Auth**: Required (`[Authorize]`)
* **Headers**: `Authorization: Bearer <access_token>`

### Response (`200 OK`)
```json
{
  "userId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "email": "employer@talentx.com",
  "roles": [
    "Employer"
  ],
  "hasPassword": true,
  "authProviders": [
    "password"
  ]
}
```

---

## 4. User Logout

Invalidates active user refresh tokens.

* **Method**: `POST`
* **Route**: `/api/v1/auth/logout`
* **Auth**: Public / Bearer
* **Response**: `200 OK`

---

## 5. Change Password

Updates the password for authenticated users (Employers, Talents, or Guests). If invoked by a candidate holding the `Guest` role, automatically graduates the user to `Talent` in both ASP.NET Identity and Domain tables, marking them verified.

* **Method**: `POST`
* **Route**: `/api/v1/auth/change-password`
* **Auth**: Required (`[Authorize(Roles = "Employer,Talent,Guest")]`)
* **Headers**: `Authorization: Bearer <access_token>`

### Request Body
```json
{
  "oldPassword": "TemporaryPassword123!",
  "newPassword": "NewSecurePassword123!"
}
```

### Response (`200 OK`)
```json
{
  "message": "Password changed successfully."
}
```

---

## 6. Add Password (Google-Registered Accounts)

Allows users who registered exclusively through Google OAuth to set a local password, enabling dual-method authentication (Google and email/password). Invalidates all other active sessions across devices while preserving the caller's session.

* **Method**: `POST`
* **Route**: `/api/v1/auth/add-password`
* **Auth**: Required (`[Authorize]`)
* **Headers**: `Authorization: Bearer <access_token>`

### Request Body
```json
{
  "newPassword": "SecurePassword123!",
  "confirmPassword": "SecurePassword123!",
  "currentRefreshToken": "current-refresh-token-from-cookie"
}
```

### Response
* `204 No Content`: Password successfully set and other sessions revoked.

---

## 7. Forgot Password

Initiates the password recovery flow. If the account was registered exclusively via Google, instructs the user to sign in with Google without sending a reset email. For local/password accounts, dispatches a secure 15-minute single-use reset link via email.

* **Method**: `POST`
* **Route**: `/api/v1/auth/forgot-password`
* **Auth**: Public (`[AllowAnonymous]`)

### Request Body
```json
{
  "email": "user@example.com"
}
```

### Response (`200 OK`)
```json
{
  "success": true,
  "provider": "Local",
  "message": "If an account exists, instructions have been sent."
}
```
*(For Google-only accounts: `success: false`, `provider: "Google"`, `message: "This account is registered with Google. Please sign in with Google."`)*

---

## 8. Validate Password Reset Token

Checks whether a password reset token is valid, unexpired, and not yet used before presenting the password reset form.

* **Method**: `GET`
* **Route**: `/api/v1/auth/validate-reset-token?email={email}&token={token}`
* **Auth**: Public (`[AllowAnonymous]`)

### Query Parameters
* `email` (string, required): The user's email address.
* `token` (string, required): The password reset token.

### Response (`200 OK`)
```json
{
  "isValid": true
}
```

---

## 9. Reset Password

Resets an account password using a single-use secure reset token received via email. If the account is in the `Guest` role (candidate provisioned via job application), graduating to `Talent` and marking verified is performed automatically upon successful reset. All existing refresh tokens for the user are revoked upon reset.

* **Method**: `POST`
* **Route**: `/api/v1/auth/reset-password`
* **Auth**: Public (`[AllowAnonymous]`)

### Request Body
```json
{
  "email": "user@example.com",
  "token": "base64-url-encoded-reset-token",
  "newPassword": "NewSecurePassword123!",
  "confirmPassword": "NewSecurePassword123!"
}
```

### Response
* `204 No Content`: Password successfully reset.

---

## 9. Verify Guest Application Email

Verifies a guest candidate's job application email using the verification link token sent after applying to a job. Activates pending applications, provisions the candidate user account (if new), dispatches credentials, and returns JWT session tokens.

* **Method**: `POST`
* **Route**: `/api/v1/auth/verify-email`
* **Auth**: Public (`[AllowAnonymous]`)

### Request Body
```json
{
  "token": "application-verification-token"
}
```

### Response (`200 OK`)
```json
{
  "userId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "email": "candidate@example.com",
  "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6...",
  "refreshToken": "4a71b2d0...",
  "requiresPasswordChange": true
}
```

`requiresPasswordChange` is `true` when a new Guest account was provisioned for this email — a temporary password was dispatched via the credentials email, and the client must force the user through the change-password flow before granting normal access. It is `false` when the candidate already had an account (re-verification of an already-provisioned user).

---

## 10. Send Delete Account OTP

Emails a 6-digit, single-use verification code to the **authenticated** user's own address, used to confirm a permanent account deletion. No email is accepted in the request body, so the code can only ever be sent to the caller. Codes are keyed separately from signup OTPs, expire after 2 minutes, and are invalidated for 10 minutes after 5 failed verification attempts. A resend is rejected for 60 seconds after a successful send, and the endpoint is rate-limited to **5 requests per 10 minutes per authenticated user** (ASP.NET fixed-window limiter, policy `DeleteAccountOtp`).

* **Method**: `POST`
* **Route**: `/api/v1/auth/delete-account/send-otp`
* **Auth**: `[Authorize(Roles = "Employer,Talent")]`

### Request Body
None.

### Response
* `204 No Content`: Code generated and dispatched.

### Error Responses
* `400 Bad Request`: A code was sent within the last 60 seconds (`Please wait {n} seconds…`).
* `401 Unauthorized`: Missing or invalid access token.
* `403 Forbidden`: Caller is neither `Employer` nor `Talent`.
* `429 Too Many Requests`: Rate limit (`DeleteAccountOtp`) exceeded; includes a `Retry-After` header. No OTP is generated or emailed.

---

## 11. Delete Account

Permanently (hard) deletes the authenticated user's account and all owned domain data — profile, resume, work experience, education, saved jobs, applications, invitations, interview invitations, interview preparations, job reports, company reviews, company, and (for employers) every job posting including soft-deleted ones, along with moderator histories, reports, and preparations attached to those jobs. The identity row is then removed, all refresh tokens are revoked, and a confirmation email is sent. The operation is single-use per code; a second call requires a new code.

* **Method**: `POST`
* **Route**: `/api/v1/auth/delete-account`
* **Auth**: `[Authorize(Roles = "Employer,Talent")]`

### Request Body
```json
{
  "otp": "123456"
}
```

### Response (`200 OK`)
```json
{
  "email": "user@example.com"
}
```

### Error Responses
* `400 Bad Request`: OTP is missing/malformed, invalid, expired, or the attempt limit is exceeded.
* `401 Unauthorized`: Missing or invalid access token.
* `403 Forbidden`: Caller is neither `Employer` nor `Talent`.
* `502 Bad Gateway`: Domain data was deleted but the sign-in (identity) cleanup failed; retry with a new code to finish.
