using System.Net;

namespace TalentX.Application.Common.Exceptions;

public class BadGatewayException : AppException
{
    public BadGatewayException(string message) : base(message) { }
    public override HttpStatusCode StatusCode => HttpStatusCode.BadGateway;
}
