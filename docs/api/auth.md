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
