# Job Listing and Job Details Walkthrough

This document provides a technical walkthrough of the Job Listing and Job Details read features in the TalentX Backend.

## Overview

The feature exposes endpoints for retrieving job information:
1. **Job Listing**: Retrieves a paginated list of published jobs.
2. **Job Details**: Retrieves the full details of a specific job by its unique ID.

The implementation strictly follows the principles of Clean Architecture and CQRS (Command Query Responsibility Segregation) using the MediatR library.

## Architecture & Layers

### 1. API Layer (`JobsController`)
Located in `TalentX.Api/Controllers/JobsController.cs`.
- The controller extends `BaseApiController` and uses `ISender` (from MediatR) to dispatch queries to the Application layer.
- `GET /api/jobs`: Accepts `page` and `pageSize` as query parameters. Dispatches a `GetPublishedJobsQuery`.
- `GET /api/jobs/{id}`: Accepts a `Guid` id parameter from the route path. Dispatches a `GetJobDetailsQuery`.

### 2. Application Layer (CQRS)
Located within `TalentX.Application/Features/Jobs/Queries`.

#### GetPublishedJobsQuery
- **Request**: `GetPublishedJobsQuery(int Page, int PageSize)`
- **Response**: `PagedResult<JobListItemDto>`
- **Handler**: `GetPublishedJobsQueryHandler` retrieves the jobs from the repository based on pagination parameters. It maps the returned Domain Entities to `JobListItemDto` using the `ToListItemDto()` extension method, and wraps them in a `PagedResult` envelope containing the total count for the frontend pagination component.

#### GetJobDetailsQuery
- **Request**: `GetJobDetailsQuery(Guid Id)`
- **Response**: `JobDetailsDto?`
- **Handler**: `GetJobDetailsQueryHandler` retrieves a specific published job by its ID. If found, it maps the entity to `JobDetailsDto` using the `ToDetailsDto()` extension method.

### 3. Data Transfer Objects (DTOs) & Mappings
To maintain clean separation between our internal domain entities and our external API contracts, we use DTOs:
- `JobListItemDto`: Contains summary information (e.g., Title, Company Name, Location) optimized for list views.
- `JobDetailsDto`: Contains comprehensive information including the full job description, expiration date, and other relevant fields.
- **Mappings**: Handled explicitly via extension methods (e.g., `ToDetailsDto()`). This explicit mapping strategy is preferred over reflection-based mappers (like AutoMapper) for better compile-time safety and performance.

### 4. Infrastructure & Persistence Layer
- **Repository Pattern**: The handlers interact with the database via `IJobRepository` (accessed through `IUnitOfWork`), which exposes methods like `GetPublishedJobsAsync(page, pageSize)` and `GetPublishedByIdAsync(id)`.
- By using `IUnitOfWork`, the Application layer remains completely decoupled from the actual Entity Framework Core implementation details.

## Execution Flow Summary

1. **Client Request**: Client makes an HTTP `GET /api/jobs` request.
2. **Controller**: `JobsController` instantiates `GetPublishedJobsQuery` and sends it via MediatR.
3. **Handler**: `GetPublishedJobsQueryHandler` receives the query and asks `_unitOfWork.Jobs` for the requested page of data.
4. **Database**: Entity Framework translates the request into SQL, runs the query, and returns Domain Entities.
5. **Mapping**: The Handler maps the Domain Entities to `JobListItemDto` objects.
6. **Response**: The Controller automatically serializes the returned `PagedResult` to JSON and responds with a 200 OK.
