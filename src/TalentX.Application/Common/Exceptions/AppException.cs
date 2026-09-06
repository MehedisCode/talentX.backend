using System.Net;

namespace TalentX.Application.Common.Exceptions;

public abstract class AppException : Exception
{
    public string? ResourceId { get; }

    protected AppException(string message, string? resourceId = null) : base(message)
    {
        ResourceId = resourceId;
    }

    public abstract HttpStatusCode StatusCode { get; }
}

