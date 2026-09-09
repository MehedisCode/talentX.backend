using TalentX.Application.Common.Models;
using TalentX.Domain.Enums;

namespace TalentX.Application.Features.Jobs.DTOs;

public sealed record GetJobsQueryParams(
    string? Keyword = null,
    string? Location = null,
    EmploymentType? EmploymentType = null,
    ExperienceLevel? ExperienceLevel = null,
    WorkArrangement? WorkArrangement = null,
    int Page = PaginationRequest.DefaultPageNumber,
    int PageSize = PaginationRequest.DefaultPageSize)
{
    public GetJobsQueryParams() : this(
        Keyword: null,
        Location: null,
        EmploymentType: null,
        ExperienceLevel: null,
        WorkArrangement: null,
        Page: PaginationRequest.DefaultPageNumber,
        PageSize: PaginationRequest.DefaultPageSize)
    {
    }
}
