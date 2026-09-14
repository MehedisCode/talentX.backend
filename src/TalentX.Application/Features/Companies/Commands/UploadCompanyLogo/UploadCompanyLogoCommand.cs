using MediatR;
using TalentX.Application.Common.Interfaces;

namespace TalentX.Application.Features.Companies.Commands.UploadCompanyLogo;

public sealed record UploadCompanyLogoCommand(IUploadedFile File) : IRequest<UploadCompanyLogoResponse>;

public sealed record UploadCompanyLogoResponse(string StorageKey, string LogoUrl);
