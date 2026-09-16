using AutoMapper;
using MediatR;
using TalentX.Application.Common.Exceptions;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Features.AdminEmployerVerification.DTOs;

namespace TalentX.Application.Features.AdminEmployerVerification.Queries.GetEmployerDetails;

public sealed class GetEmployerDetailsQueryHandler
    : IRequestHandler<GetEmployerDetailsQuery, EmployerDetailDto?>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public GetEmployerDetailsQueryHandler(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<EmployerDetailDto?> Handle(
        GetEmployerDetailsQuery request,
        CancellationToken cancellationToken)
    {
        var employer = await _unitOfWork.Employers.GetByIdAsync(request.EmployerId, cancellationToken)
            ?? throw new NotFoundException(nameof(Domain.Employer), request.EmployerId);

        return _mapper.Map<EmployerDetailDto>(employer);
    }
}
