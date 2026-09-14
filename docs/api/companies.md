# Companies API (Public)

The Companies API provides public endpoints to view company profiles and their published active job vacancies without authentication.

---

## 1. Get Public Company Profile

Returns publicly accessible company profile details along with active published vacancies count and listings.

* **Method**: `GET`
* **Route**: `/api/v1/companies/{id}`
* **Auth**: Public (`[AllowAnonymous]`)

### Parameters
* `id` (path): `Guid` - The unique company ID.

### Response (`200 OK`)
```json
{
  "id": "7b4f8c92-3456-4abc-9876-123456789abc",
  "name": "Cyberdyne Systems",
  "logoUrl": "https://...",
  "description": "Pioneering cybernetics and artificial intelligence solutions.",
  "industry": "Information Technology",
  "companySize": "501-1000",
  "website": "https://cyberdyne.com",
  "location": "Sunnyvale, CA",
  "socialLinks": {
    "linkedIn": "https://linkedin.com/company/cyberdyne",
    "twitter": "https://x.com/cyberdyne",
    "facebook": null,
    "gitHub": "https://github.com/cyberdyne",
    "website": "https://cyberdyne.com"
  },
  "activeJobsCount": 2,
  "publishedJobs": [
    {
      "id": "e81f76c3-1234-4abc-9876-123456789def",
      "title": "Senior AI Systems Engineer",
      "location": "Sunnyvale, CA",
      "city": "Sunnyvale",
      "country": "United States",
      "employmentType": "FullTime",
      "workArrangement": "Hybrid",
      "experienceLevel": "SeniorLevel",
      "vacancies": 2,
      "expireAt": "2026-10-31T23:59:59Z",
      "publishedAt": "2026-09-01T10:00:00Z"
    }
  ]
}
```

### Error Responses
* `404 Not Found`: Company does not exist.

---

## 2. Get Company Published Jobs

Returns only the active, published, and non-expired job vacancies for a company. Draft, closed, deleted, or expired jobs are excluded.

* **Method**: `GET`
* **Route**: `/api/v1/companies/{id}/jobs`
* **Auth**: Public (`[AllowAnonymous]`)

### Parameters
* `id` (path): `Guid` - The unique company ID.

### Response (`200 OK`)
```json
[
  {
    "id": "e81f76c3-1234-4abc-9876-123456789def",
    "title": "Senior AI Systems Engineer",
    "location": "Sunnyvale, CA",
    "city": "Sunnyvale",
    "country": "United States",
    "employmentType": "FullTime",
    "workArrangement": "Hybrid",
    "experienceLevel": "SeniorLevel",
    "vacancies": 2,
    "expireAt": "2026-10-31T23:59:59Z",
    "publishedAt": "2026-09-01T10:00:00Z"
  }
]
```

### Error Responses
* `404 Not Found`: Company does not exist.
