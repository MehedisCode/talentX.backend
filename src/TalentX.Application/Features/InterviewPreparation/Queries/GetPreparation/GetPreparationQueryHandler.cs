using MediatR;
using Microsoft.Extensions.Options;
using TalentX.Application.Common.Exceptions;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Features.InterviewPreparation.DTOs;
using TalentX.Application.Features.InterviewPreparation.Mappers;
using TalentX.Application.Identity.Services;

namespace TalentX.Application.Features.InterviewPreparation.Queries.GetPreparation;

public sealed class GetPreparationQueryHandler
    : IRequestHandler<GetPreparationQuery, InterviewPreparationDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;
    private readonly InterviewPreparationSettings _settings;

    public GetPreparationQueryHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUser,
        IOptions<InterviewPreparationSettings> settings)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _settings = settings.Value;
    }

    public async Task<InterviewPreparationDto> Handle(
        GetPreparationQuery request, CancellationToken ct)
    {
        var userId = _currentUser.UserId!;

        var preparation = await _unitOfWork.InterviewPreparations
            .GetByInvitationIdAsync(request.InvitationId, ct)
            ?? throw new NotFoundException("Interview preparation not found.");

        if (preparation.TalentUserId != userId)
            throw new ForbiddenException("You do not have access to this interview preparation.");

        var answers = await _unitOfWork.InterviewPreparations
            .GetAnswersByPreparationIdAsync(preparation.Id, ct);
        var evaluation = await _unitOfWork.InterviewPreparations
            .GetEvaluationByPreparationIdAsync(preparation.Id, ct);

        return InterviewPreparationMapper.ToDto(
            preparation, _settings.AccessWindowAfterInterview, answers, evaluation);
    }
}
