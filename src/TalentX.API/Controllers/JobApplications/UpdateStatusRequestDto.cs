using TalentX.Domain.Enums;

namespace TalentX.API.Controllers.JobApplications;

public sealed record UpdateStatusRequestDto(ApplicationStatus NewStatus);
