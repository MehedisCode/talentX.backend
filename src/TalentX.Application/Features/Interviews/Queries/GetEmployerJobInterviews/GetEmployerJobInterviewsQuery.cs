using MediatR;
using TalentX.Application.Common.Models;
using TalentX.Application.Features.Interviews.DTOs;
using TalentX.Domain.Enums;

namespace TalentX.Application.Features.Interviews.Queries.GetEmployerJobInterviews;

public sealed record GetEmployerJobInterviewsQuery(
    Guid JobId,
    InterviewInvitationStatus? Status = null,
    InterviewType? Type = null,
    int Page = PaginationRequest.DefaultPageNumber,
    int PageSize = PaginationRequest.DefaultPageSize
) : IRequest<PaginatedList<EmployerInterviewDto>>;
