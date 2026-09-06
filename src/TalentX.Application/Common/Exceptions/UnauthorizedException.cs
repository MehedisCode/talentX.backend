using System.Net;

namespace TalentX.Application.Common.Exceptions;

public class UnauthorizedException : AppException
{
    public UnauthorizedException(string message, string? resourceId = null) : base(message, resourceId) { }
    public override HttpStatusCode StatusCode => HttpStatusCode.Unauthorized;
}