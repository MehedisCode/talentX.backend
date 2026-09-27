using MediatR;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Features.InterviewPreparation.DTOs;
using TalentX.Application.Features.InterviewPreparation.Mappers;
using TalentX.Application.Identity.Services;

namespace TalentX.Application.Features.InterviewPreparation.Queries.GetPreparationsByTalent;

public sealed class GetPreparationsByTalentQueryHandler
    : IRequestHandler<GetPreparationsByTalentQuery, IReadOnlyList<InterviewPreparationDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;

    public GetPreparationsByTalentQueryHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUser)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<IReadOnlyList<InterviewPreparationDto>> Handle(
        GetPreparationsByTalentQuery request, CancellationToken ct)
    {
        var userId = _currentUser.UserId!;

        var preparations = await _unitOfWork.InterviewPreparations
            .GetByTalentUserIdAsync(userId, ct);

        return preparations.Select(InterviewPreparationMapper.ToDto).ToList();
    }
}
