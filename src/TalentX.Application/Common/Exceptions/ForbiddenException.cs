using System.Net;

namespace TalentX.Application.Common.Exceptions;

public class ForbiddenException : AppException
{
    public ForbiddenException(string message, string? resourceId = null) : base(message, resourceId) { }
    public override HttpStatusCode StatusCode => HttpStatusCode.Forbidden;
}