# Employers API

The Employers API enables employer registration, OTP verification, resending OTP, and Google OAuth 2.0 onboarding.

---

## 1. Register Employer

Creates a new Employer account with `UserRole.Employer`, generates a 6-digit OTP, and dispatches a verification email.

* **Method**: `POST`
* **Route**: `/api/v1/employers/register`
* **Auth**: Public (`[AllowAnonymous]`)

### Request Body
```json
{
  "name": "Sarah Connor",
  "email": "sarah.connor@cyberdyne.com",
  "password": "Password123!",
  "companyName": "Cyberdyne Systems"
}
```

### Response (`201 Created`)
```json
{
  "id": "c1f7b8d4-3456-4abc-9876-123456789abc",
  "name": "Sarah Connor",
  "email": "sarah.connor@cyberdyne.com",
  "companyName": "Cyberdyne Systems",
  "role": "Employer",
  "isVerified": false,
  "createdAt": "2026-08-19T10:00:00Z"
}
```

### Error Responses
* `400 Bad Request`: Validation failure.
* `409 Conflict`: Email is already registered and verified.

---

## 2. Verify Email (OTP)

Validates the 6-digit OTP sent to the employer's email and marks the account as verified.

* **Method**: `POST`
* **Route**: `/api/v1/employers/verify-email`
* **Auth**: Public (`[AllowAnonymous]`)

### Request Body
```json
{
  "email": "sarah.connor@cyberdyne.com",
  "otp": "123456"
}
```

### Response (`200 OK`)
```json
{
  "message": "Email verified successfully."
}
```

---

## 3. Resend OTP

Generates and sends a new 6-digit verification code to an unverified employer account.

* **Method**: `POST`
* **Route**: `/api/v1/employers/resend-otp`
* **Auth**: Public (`[AllowAnonymous]`)

### Request Body
```json
{
  "email": "sarah.connor@cyberdyne.com"
}
```

### Response (`200 OK`)
```json
{
  "message": "A new verification code has been sent to your email."
}
```

---

## 4. Google Sign-In & Onboarding

Authenticates an employer via a Google OAuth ID token. If the account does not exist, automatically provisions an Employer profile.

* **Method**: `POST`
* **Route**: `/api/v1/employers/google`
* **Auth**: Public (`[AllowAnonymous]`)

### Request Body
```json
{
  "idToken": "eyJhbGciOiJSUzI1NiIsImtpZCI6...",
  "companyName": "Cyberdyne Systems"
}
```

### Response (`200 OK`)
```json
{
  "id": "c1f7b8d4-3456-4abc-9876-123456789abc",
  "name": "Sarah Connor",
  "email": "sarah.connor@cyberdyne.com",
  "companyName": "Cyberdyne Systems",
  "role": "Employer",
  "isVerified": true,
  "createdAt": "2026-08-19T10:00:00Z"
}
```
