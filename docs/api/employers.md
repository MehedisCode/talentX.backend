# Employers API (`/api/v1/employers`)

Controller: [`EmployersController`](file:///Users/jubair/Developer/asthait/talentx/talentx2026q3.backend/src/TalentX.Api/Controllers/EmployersController.cs)  
Authorization: `[AllowAnonymous]`

Handles all actions related to employer account onboarding, registration, Google authentication, and email OTP verification.

---

## 1. Register New Employer

Registers an employer with email and password, creates the Identity user, persists the domain entity, and dispatches a 6-digit verification OTP email.

- **URL**: `/api/v1/employers`
- **Method**: `POST`
- **Headers**: `Content-Type: application/json`

### Request Body
```json
{
  "name": "Sarah Connor",
  "email": "sarah@cyberdyne.ai",
  "companyName": "Cyberdyne Systems",
  "password": "SecurePassword123!",
  "confirmPassword": "SecurePassword123!"
}
```

### Validation Constraints:
- `name`: Required, max 120 characters.
- `email`: Required, valid email format, max 255 characters.
- `companyName`: Required, max 200 characters.
- `password`: Min 8 characters, at least 1 uppercase letter, 1 lowercase letter, 1 digit, and 1 special character (`! ? * @ # $ % ^ & + =`).
- `confirmPassword`: Must match `password`.

### Response `201 Created`
```json
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "userId": "d7b4e3e2-8b41-48e2-b7b5-1234567890ab",
  "name": "Sarah Connor",
  "email": "sarah@cyberdyne.ai",
  "companyName": "Cyberdyne Systems",
  "role": "Employer",
  "isVerified": false,
  "createdAt": "2026-08-18T10:00:00Z",
  "updatedAt": null
}
```

### Error Responses:
- `400 Bad Request`: Input validation failed.
- `409 Conflict`: An employer with this email is already registered and verified.

---

## 2. Google OAuth Sign-In

Authenticates or registers an employer account using a verified Google OAuth ID Token.

- **URL**: `/api/v1/employers/google`
- **Method**: `POST`
- **Headers**: `Content-Type: application/json`

### Request Body
```json
{
  "idToken": "eyJhbGciOiJSUzI1NiIsImtpZCI6IjEy..."
}
```

### Response `200 OK`
```json
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "userId": "d7b4e3e2-8b41-48e2-b7b5-1234567890ab",
  "name": "Sarah Connor",
  "email": "sarah@cyberdyne.ai",
  "companyName": "Cyberdyne Systems",
  "role": "Employer",
  "isVerified": true,
  "createdAt": "2026-08-18T10:00:00Z",
  "updatedAt": "2026-08-18T10:05:00Z"
}
```

### Error Responses:
- `400 Bad Request`: Missing ID token.
- `401 Unauthorized`: Google token signature is invalid, expired, or rejected.

---

## 3. Verify Email with OTP

Validates the 6-digit OTP received in email to mark the employer account as verified.

- **URL**: `/api/v1/employers/verify-email`
- **Method**: `POST`
- **Headers**: `Content-Type: application/json`

### Request Body
```json
{
  "email": "sarah@cyberdyne.ai",
  "otp": "481920"
}
```

### Response `200 OK`
```json
{
  "message": "Email verified successfully."
}
```

### Error Responses:
- `400 Bad Request`: Invalid or expired OTP code.
- `404 Not Found`: No employer registered with the provided email.

---

## 4. Resend Verification OTP

Generates and emails a new 6-digit OTP code for an unverified employer account.

- **URL**: `/api/v1/employers/resend-otp`
- **Method**: `POST`
- **Headers**: `Content-Type: application/json`

### Request Body
```json
{
  "email": "sarah@cyberdyne.ai"
}
```

### Response `200 OK`
```json
{
  "message": "A new verification code has been sent to your email."
}
```

### Error Responses:
- `400 Bad Request`: The account is already verified.
- `404 Not Found`: Employer not found.
