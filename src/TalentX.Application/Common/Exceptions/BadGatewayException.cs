using System.Net;

namespace TalentX.Application.Common.Exceptions;

public class BadGatewayException : AppException
{
    public BadGatewayException(string message, string? resourceId = null) : base(message, resourceId) { }
    public override HttpStatusCode StatusCode => HttpStatusCode.BadGateway;
}
