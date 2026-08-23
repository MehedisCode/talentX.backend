using AutoMapper;
using MediatR;
using TalentX.Application.Common.Exceptions;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Features.Jobs.DTOs;

namespace TalentX.Application.Features.Jobs.Queries;

public record GetJobDetailsQuery(Guid Id) : IRequest<JobDetailsDto>;

public class GetJobDetailsQueryHandler : IRequestHandler<GetJobDetailsQuery, JobDetailsDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public GetJobDetailsQueryHandler(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<JobDetailsDto> Handle(GetJobDetailsQuery request, CancellationToken cancellationToken)
    {
        var job = await _unitOfWork.Jobs.GetPublishedJobByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException($"Job '{request.Id}' was not found.");

        return _mapper.Map<JobDetailsDto>(job);
    }
}
