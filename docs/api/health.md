# Health & Diagnostics API (`/api/v1/health`)

Controller: [`HealthController`](file:///Users/jubair/Developer/asthait/talentx/talentx2026q3.backend/src/TalentX.Api/Controllers/HealthController.cs)

Provides uptime metrics, server date/time in UTC, and system operational health.

---

## 1. System Health & Uptime

Returns the operational status, current server time, process start timestamp, and total formatted uptime.

- **URL**: `/api/v1/health`
- **Method**: `GET`
- **Authorization**: `[Authorize(Roles = "Employer")]` (or `[AllowAnonymous]` during load balancer probing).

### Response `200 OK`
```json
{
  "status": "Healthy",
  "serverDate": "2026-08-18",
  "serverTimeUtc": "2026-08-18T17:00:00.1234567Z",
  "startTimeUtc": "2026-08-18T10:00:00.0000000Z",
  "uptime": "0d 07h 00m 00s",
  "uptimeSeconds": 25200.12
}
```
