# Jobs API

The Jobs API lets an authenticated Employer create, save as a draft, publish, and edit job listings.

---

## 1. Create Job

Creates a new job listing owned by the calling employer, either as a `Draft` or immediately `Published` depending on the `publish` flag.

* **Method**: `POST`
* **Route**: `/api/v1/Jobs`
* **Auth**: Required (`[Authorize(Roles = "Employer")]`)
* **Headers**: `Authorization: Bearer <access_token>`

### Request Body
```json
{
  "title": "Backend Engineer",
  "companyName": "Acme Corp",
  "description": "Build and maintain backend services powering the TalentX platform.",
  "employmentType": "FullTime",
  "experienceLevel": "MidLevel",
  "location": "Dhaka",
  "workArrangement": "Remote",
  "requiredSkills": ["C#", "SQL", "ASP.NET Core"],
  "vacancies": 2,
  "expireAt": "2026-12-31T00:00:00Z",
  "publish": false
}
```

### Field Rules
| Field | Rules |
| :--- | :--- |
| `title` | Required, 5–200 chars (trimmed), can't start with a digit, must contain a letter |
| `companyName` | Required, 2–200 chars (trimmed), must contain a letter |
| `description` | Required, 20–10000 chars (trimmed), must contain a letter |
| `employmentType` | One of `FullTime`, `PartTime`, `Contract`, `Internship` |
| `experienceLevel` | One of `EntryLevel`, `MidLevel`, `SeniorLevel`, `Lead`, `Executive` |
| `location` | Required, 5–200 chars (trimmed), can't be purely numeric |
| `workArrangement` | One of `OnSite`, `Remote`, `Hybrid` |
| `requiredSkills` | Each entry must contain a letter (blank or punctuation/digit-only entries are rejected) |
| `vacancies` | `>= 0` |
| `expireAt` | Must be in the future |
| `publish` | `true` publishes immediately (subject to the completeness checks below); `false` saves as `Draft` |

If `publish: true`, the job must also satisfy `Job.EnsureIsPublishable()` — all fields non-blank, `requiredSkills` non-empty, `vacancies > 0`, `expireAt` in the future. This is a domain-level check separate from the request validation above.

### Response (`201 Created`)
```json
"3fa85f64-5717-4562-b3fc-2c963f66afa6"
```
The new job's `id` (a `Guid`), returned as the raw response body.

### Error Responses
* `400 Bad Request`: Validation failure, or `publish: true` on an incomplete listing.
* `401 Unauthorized`: Missing, invalid, or expired token.

---

## 2. Update Job

Partially updates an existing job listing. Only the fields present in the request are changed — omitted (`null`) fields are left as-is. Can also be used to publish a `Draft` by setting `publish: true`.

* **Method**: `PATCH`
* **Route**: `/api/v1/Jobs/{id}`
* **Auth**: Required (`[Authorize(Roles = "Employer")]`) — the caller must be the job's owning employer.

### Request Body
All fields are optional except `publish`; send only what you want to change.
```json
{
  "title": "Senior Backend Engineer",
  "vacancies": 3,
  "publish": true
}
```

### Field Rules
Same content rules as [Create Job](#1-create-job) apply to any field that is sent (a `null` field is skipped, not validated).

### Response (`204 No Content`)
No response body.

### Error Responses
* `400 Bad Request`: Validation failure, or `publish: true` on a listing that's still incomplete.
* `401 Unauthorized`: Missing, invalid, or expired token.
* `403 Forbidden`: The caller is authenticated but does not own this job.
* `404 Not Found`: No job exists with the given `id`.

---

## Job Status Lifecycle

```
Draft ──(publish: true, all fields valid)──> Published
```

A job is created as `Draft` by default. Once `Published`, `EnsureIsPublishable()` will reject a second publish attempt (`Only draft jobs can be published.`) — there is currently no endpoint to un-publish or close a listing.
