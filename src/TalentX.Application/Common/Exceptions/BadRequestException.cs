using System.Net;

namespace TalentX.Application.Common.Exceptions;

public class BadRequestException : AppException
{
    public BadRequestException(string message, string? resourceId = null) : base(message, resourceId) { }
    public override HttpStatusCode StatusCode => HttpStatusCode.BadRequest;
}