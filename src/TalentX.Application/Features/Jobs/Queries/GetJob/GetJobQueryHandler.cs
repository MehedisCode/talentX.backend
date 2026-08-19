using MediatR;
using TalentX.Application.Common.Interfaces;

namespace TalentX.Application.Features.Jobs.Queries.GetJob;

public class GetJobQueryHandler : IRequestHandler<GetJobQuery, JobDto?>
{
    private readonly IJobRepository _jobRepository;

    public GetJobQueryHandler(IJobRepository jobRepository)
    {
        _jobRepository = jobRepository;
    }

    public async Task<JobDto?> Handle(GetJobQuery request, CancellationToken cancellationToken)
    {
        var job = await _jobRepository.GetByIdAsync(request.Id, cancellationToken);

        return job?.ToDto();
    }
}
