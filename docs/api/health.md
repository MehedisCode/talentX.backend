# Health & Diagnostics API

The Health API provides lightweight diagnostic monitoring for uptime checks, container orchestrators (Kubernetes liveness/readiness probes), and deployment verification.

---

## 1. System Health Check

Returns the operational status, current UTC time, process start time, and human-readable uptime.

* **Method**: `GET`
* **Route**: `/health` or `/api/v1/health`
* **Auth**: Public (`[AllowAnonymous]`)

### Response (`200 OK`)
```json
{
  "status": "Healthy",
  "serverDate": "2026-08-19",
  "serverTimeUtc": "2026-08-19T10:30:00.123456Z",
  "startTimeUtc": "2026-08-19T08:00:00.000000Z",
  "uptime": "0d 02h 30m 00s",
  "uptimeSeconds": 9000.12
}
```
