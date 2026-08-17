using AutoMapper;
using MediatR;
using TalentX.Application.Common.Interfaces;

namespace TalentX.Application.Jobs.Queries.GetJob;

public class GetJobQueryHandler : IRequestHandler<GetJobQuery, JobDto?>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public GetJobQueryHandler(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<JobDto?> Handle(GetJobQuery request, CancellationToken cancellationToken)
    {
        var job = await _unitOfWork.Jobs.GetByIdAsync(request.Id, cancellationToken);

        return job == null ? null : _mapper.Map<JobDto>(job);
    }
}
