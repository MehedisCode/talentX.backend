using MediatR;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Features.InterviewPreparation.DTOs;
using TalentX.Application.Identity.Services;
using TalentX.Domain;
using TalentX.Domain.Entities;

namespace TalentX.Application.Features.InterviewPreparation.Commands.UpdateInterviewPreparationConfig;

public sealed class UpdateInterviewPreparationConfigCommandHandler
    : IRequestHandler<UpdateInterviewPreparationConfigCommand, InterviewPreparationConfigDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;

    public UpdateInterviewPreparationConfigCommandHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUser)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<InterviewPreparationConfigDto> Handle(
        UpdateInterviewPreparationConfigCommand request, CancellationToken ct)
    {
        var config = await _unitOfWork.InterviewPreparationConfigs.GetAsync(ct);

        if (config is null)
        {
            config = InterviewPreparationConfig.CreateDefault(
                request.DailyGenerateLimit,
                request.DailyGenerationLimit,
                request.MaxRegenerationCount,
                request.AccessWindowAfterInterviewMinutes);

            await _unitOfWork.InterviewPreparationConfigs.AddAsync(config, ct);
        }
        else
        {
            config.Update(
                request.DailyGenerateLimit,
                request.DailyGenerationLimit,
                request.MaxRegenerationCount,
                request.AccessWindowAfterInterviewMinutes);

            _unitOfWork.InterviewPreparationConfigs.Update(config);
        }

        var actorEmail = _currentUser.Email!;
        await _unitOfWork.AuditLogs.AddAsync(
            new AuditLog(actorEmail,
                $"Updated interview preparation config: " +
                $"DailyGenerateLimit={request.DailyGenerateLimit}, " +
                $"DailyGenerationLimit={request.DailyGenerationLimit}, " +
                $"MaxRegenerationCount={request.MaxRegenerationCount}, " +
                $"AccessWindowAfterInterviewMinutes={request.AccessWindowAfterInterviewMinutes}"),
            ct);

        await _unitOfWork.SaveChangesAsync(ct);

        return new InterviewPreparationConfigDto(
            config.Id,
            config.DailyGenerateLimit,
            config.DailyGenerationLimit,
            config.MaxRegenerationCount,
            config.AccessWindowAfterInterviewMinutes,
            config.CreatedAt,
            config.UpdatedAt);
    }
}
