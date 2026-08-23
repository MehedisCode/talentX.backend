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
  "employer": {
    "id": "c1f7b8d4-3456-4abc-9876-123456789abc",
    "name": "Sarah Connor",
    "email": "sarah.connor@cyberdyne.com",
    "companyName": "Cyberdyne Systems",
    "role": "Employer",
    "isVerified": true,
    "createdAt": "2026-08-19T10:00:00Z"
  },
  "accessToken": "eyJhbGciOiJIUzI1NiIs...",
  "refreshToken": "7f8a9b0c-1234-4567-8901-abcdef123456"
}
```

### Error Responses
* `400 Bad Request`: Validation failure.
* `409 Conflict`: Email is already registered and verified.

---

## 2. Verify Email (OTP)

Validates the 6-digit OTP sent to the employer's email. If the employer profile exists, marks it verified and issues JWT tokens.

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
  "employer": {
    "id": "c1f7b8d4-3456-4abc-9876-123456789abc",
    "name": "Sarah Connor",
    "email": "sarah.connor@cyberdyne.com",
    "companyName": "Cyberdyne Systems",
    "role": "Employer",
    "isVerified": true,
    "createdAt": "2026-08-19T10:00:00Z"
  },
  "accessToken": "eyJhbGciOiJIUzI1NiIs...",
  "refreshToken": "7f8a9b0c-1234-4567-8901-abcdef123456"
}
```

---

## 3. Send OTP

Generates and sends a 6-digit verification code to the specified email address. Supports both new unverified emails and existing unverified employers.

* **Method**: `POST`
* **Route**: `/api/v1/employers/send-otp`
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
  "message": "A verification code has been sent to your email."
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
