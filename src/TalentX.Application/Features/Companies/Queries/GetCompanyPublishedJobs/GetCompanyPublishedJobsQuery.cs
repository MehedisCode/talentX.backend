using System.Collections.Generic;
using MediatR;
using TalentX.Application.Features.Companies.DTOs;

namespace TalentX.Application.Features.Companies.Queries.GetCompanyPublishedJobs;

public sealed record GetCompanyPublishedJobsQuery(Guid CompanyId) : IRequest<IReadOnlyList<PublicCompanyJobDto>>;
