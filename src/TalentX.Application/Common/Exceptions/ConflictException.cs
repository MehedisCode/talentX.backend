using System.Net;

namespace TalentX.Application.Common.Exceptions;

public class ConflictException : AppException
{
    public ConflictException(string message, string? resourceId = null) : base(message, resourceId) { }
    public override HttpStatusCode StatusCode => HttpStatusCode.Conflict;
}