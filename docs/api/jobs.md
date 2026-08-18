# Jobs API (`/api/v1/jobs`)

Controller: [`JobsController`](file:///Users/jubair/Developer/asthait/talentx/talentx2026q3.backend/src/TalentX.Api/Controllers/JobsController.cs)  
Authorization: `[AllowAnonymous]`

Enables public job discovery and details retrieval for published positions.

---

## 1. List Published Jobs

Fetches a paginated list of all active jobs whose status is strictly `JobStatus.Published`.

- **URL**: `/api/v1/jobs`
- **Method**: `GET`
- **Query Parameters**:
  - `page` (integer, default `1`): Current page number.
  - `pageSize` (integer, default `20`): Number of jobs per page.

### Response `200 OK`
```json
{
  "items": [
    {
      "id": "7ca6b8c1-1e9a-4c2d-94c0-432190fedcba",
      "title": "Senior AI / ML Research Engineer",
      "companyName": "Cyberdyne Systems",
      "location": "San Francisco, CA (Remote)",
      "workMode": "Remote",
      "employmentType": "FullTime",
      "experienceLevel": "Senior",
      "minSalary": 160000,
      "maxSalary": 220000,
      "currency": "USD",
      "skills": ["Python", "PyTorch", "LLM Fine-Tuning", "Distributed Systems"],
      "publishedAt": "2026-08-15T08:30:00Z"
    }
  ],
  "page": 1,
  "pageSize": 20,
  "totalCount": 14,
  "totalPages": 1
}
```

---

## 2. Get Job Details by ID

Retrieves complete information and description for a specific job.

- **URL**: `/api/v1/jobs/{id}`
- **Method**: `GET`
- **URL Parameters**:
  - `id` (guid, required): The unique Job ID.

### Response `200 OK`
```json
{
  "id": "7ca6b8c1-1e9a-4c2d-94c0-432190fedcba",
  "title": "Senior AI / ML Research Engineer",
  "companyName": "Cyberdyne Systems",
  "description": "We are seeking a talented Senior AI Engineer to spearhead our autonomous LLM systems...",
  "responsibilities": "• Train and fine-tune large generative architectures\n• Design real-time inference pipelines",
  "requirements": "• 5+ years building production ML systems\n• Proficiency with PyTorch and CUDA optimization",
  "workMode": "Remote",
  "employmentType": "FullTime",
  "experienceLevel": "Senior",
  "minSalary": 160000,
  "maxSalary": 220000,
  "currency": "USD",
  "skills": ["Python", "PyTorch", "LLM", "Distributed Systems"],
  "status": "Published",
  "createdAt": "2026-08-15T08:00:00Z"
}
```

### Error Responses:
- `404 Not Found`: Job does not exist or has not been published yet.
