using Microsoft.AspNetCore.Http;
using TalentX.Application.Identity.Services;
using TalentX.Infrastructure.Identity.Services;

namespace TalentX.API.Identity.Services;

public sealed class DevCurrentUserService : ICurrentUserService
{
    private const string DevUserIdHeaderName = "X-Dev-User-Id";
    private const string DevUserEmailHeaderName = "X-Dev-User-Email";

    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly CurrentUserService _claimsCurrentUserService;

    public DevCurrentUserService(IHttpContextAccessor httpContextAccessor, CurrentUserService claimsCurrentUserService)
    {
        _httpContextAccessor = httpContextAccessor;
        _claimsCurrentUserService = claimsCurrentUserService;
    }

    public bool IsAuthenticated =>
        _claimsCurrentUserService.IsAuthenticated || DevUserIdHeaderValue is not null;

    public string? UserId =>
        _claimsCurrentUserService.IsAuthenticated ? _claimsCurrentUserService.UserId : DevUserIdHeaderValue;

    public string? Email =>
        _claimsCurrentUserService.IsAuthenticated ? _claimsCurrentUserService.Email : DevUserEmailHeaderValue;

    public IReadOnlyList<string> Roles => _claimsCurrentUserService.Roles;

    private string? DevUserIdHeaderValue => GetHeaderValueOrNull(DevUserIdHeaderName);

    private string? DevUserEmailHeaderValue => GetHeaderValueOrNull(DevUserEmailHeaderName);

    private string? GetHeaderValueOrNull(string headerName)
    {
        var headerValue = _httpContextAccessor.HttpContext?.Request.Headers[headerName].ToString().Trim();
        return string.IsNullOrWhiteSpace(headerValue) ? null : headerValue;
    }
}
