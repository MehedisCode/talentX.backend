using TalentX.Application.Common.Interfaces;

namespace TalentX.Infrastructure.Services;

// TEMPORARY stand-in for ICurrentUserService until JWT auth is wired up.
// Every request resolves to the same fake employer id — swap this
// registration in DependencyInjection.cs for a real implementation
// (reading the authenticated user's id off HttpContext.User) once auth lands.
public class MockCurrentUserService : ICurrentUserService
{
    public string UserId { get; set; } = "00000000-0000-0000-0000-000000000001";
}
