using System.Net;

namespace TalentX.Application.Common.Exceptions;

public class TooManyRequestsException : AppException
{
    public TooManyRequestsException(string message, string? resourceId = null)
        : base(message, resourceId)
    {
    }

    public override HttpStatusCode StatusCode => HttpStatusCode.TooManyRequests;
}
