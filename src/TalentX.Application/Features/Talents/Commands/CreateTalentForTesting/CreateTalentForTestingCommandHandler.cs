using MediatR;
using TalentX.Application.Common.Exceptions;
using TalentX.Application.Common.Interfaces;
using TalentX.Domain;

namespace TalentX.Application.Features.Talents.Commands.CreateTalentForTesting;

public sealed class CreateTalentForTestingCommandHandler : IRequestHandler<CreateTalentForTestingCommand, Guid>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IHostEnvironmentService _hostEnvironmentService;

    public CreateTalentForTestingCommandHandler(
        IUnitOfWork unitOfWork,
        IHostEnvironmentService hostEnvironmentService)
    {
        _unitOfWork = unitOfWork;
        _hostEnvironmentService = hostEnvironmentService;
    }

    public async Task<Guid> Handle(CreateTalentForTestingCommand request, CancellationToken cancellationToken)
    {
        if (!_hostEnvironmentService.IsDevelopment)
            throw new ForbiddenException("This operation is only available in the Development environment.");

        var existingTalent = await _unitOfWork.Talents.GetByUserIdAsync(request.UserId, cancellationToken);
        if (existingTalent is not null)
            throw new ConflictException($"A Talent for user '{request.UserId}' already exists.");

        var talent = new Talent(request.UserId, request.Name, request.Email);

        await _unitOfWork.Talents.AddAsync(talent, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return talent.Id;
    }
}
