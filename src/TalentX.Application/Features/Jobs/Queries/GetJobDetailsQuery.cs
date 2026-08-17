using MediatR;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.DTOs.Jobs;
using TalentX.Application.Mappings;

namespace TalentX.Application.Features.Jobs.Queries;

public record GetJobDetailsQuery(Guid Id) : IRequest<JobDetailsDto?>;

public class GetJobDetailsQueryHandler : IRequestHandler<GetJobDetailsQuery, JobDetailsDto?>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetJobDetailsQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<JobDetailsDto?> Handle(GetJobDetailsQuery request, CancellationToken cancellationToken)
    {
        var job = await _unitOfWork.Jobs.GetPublishedByIdAsync(request.Id, cancellationToken);
        return job?.ToDetailsDto();
    }
}
