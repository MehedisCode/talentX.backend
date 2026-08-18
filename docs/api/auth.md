# Authentication API (`/api/v1/auth`)

Controller: [`AuthController`](file:///Users/jubair/Developer/asthait/talentx/talentx2026q3.backend/src/TalentX.Api/Controllers/AuthController.cs)

Manages standard email/password authentication sessions, cookie management, silent token rotation, and current session inspection.

---

## 1. User Login

Validates credentials against ASP.NET Core Identity, generates a JWT Access Token and Refresh Token, stores them in secure **HttpOnly cookies**, and returns user metadata.

- **URL**: `/api/v1/auth/login`
- **Method**: `POST`
- **Headers**: `Content-Type: application/json`

### Request Body
```json
{
  "email": "sarah@cyberdyne.ai",
  "password": "SecurePassword123!"
}
```

### Response `200 OK`
- **Set-Cookie Headers**:
  - `TalentXAccessToken=<token>; HttpOnly; Secure; SameSite=None; Max-Age=900; Path=/`
  - `TalentXRefreshToken=<token>; HttpOnly; Secure; SameSite=None; Max-Age=604800; Path=/`
- **Body**:
```json
{
  "userId": "d7b4e3e2-8b41-48e2-b7b5-1234567890ab",
  "email": "sarah@cyberdyne.ai"
}
```

### Error Responses:
- `401 Unauthorized`: Invalid email or password.

---

## 2. Refresh Token Session

Silently rotates the access token and refresh token using the incoming `TalentXRefreshToken` cookie.

- **URL**: `/api/v1/auth/refresh`
- **Method**: `POST`
- **Cookies Required**: `TalentXRefreshToken`

### Response `200 OK`
- Re-issues fresh `TalentXAccessToken` and `TalentXRefreshToken` cookie headers.

### Error Responses:
- `401 Unauthorized`: Missing, expired, or revoked refresh token. (Clears cookies).

---

## 3. Logout Session

Revokes the refresh token in the database and wipes both authentication cookies.

- **URL**: `/api/v1/auth/logout`
- **Method**: `POST`

### Response `200 OK`
- Response clears `TalentXAccessToken` and `TalentXRefreshToken` cookies.

---

## 4. Current User Profile

Returns information about the currently authenticated session via `ICurrentUserService`.

- **URL**: `/api/v1/auth/my-profile`
- **Method**: `POST`

### Response `200 OK`
```json
{
  "userId": "d7b4e3e2-8b41-48e2-b7b5-1234567890ab",
  "email": "sarah@cyberdyne.ai",
  "roles": [
    "Employer"
  ],
  "isAuthenticated": true
}
```
