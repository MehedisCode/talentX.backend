using MediatR;
using TalentX.Application.Features.TalentMatching.DTOs;

namespace TalentX.Application.Features.TalentMatching.Queries.GetCandidateSummary;

public sealed record GetCandidateSummaryQuery(string Identifier) : IRequest<CandidateProfileSummary>;
