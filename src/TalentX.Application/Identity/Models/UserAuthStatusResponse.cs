namespace TalentX.Application.Identity.Models;

public sealed record UserAuthStatusResponse(bool HasPassword, IReadOnlyList<string> AuthProviders);
