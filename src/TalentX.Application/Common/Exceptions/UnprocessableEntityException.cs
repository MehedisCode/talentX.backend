using System.Net;

namespace TalentX.Application.Common.Exceptions;

public class UnprocessableEntityException : AppException
{
    public UnprocessableEntityException(string message, string? resourceId = null) : base(message, resourceId) { }
    public override HttpStatusCode StatusCode => HttpStatusCode.UnprocessableEntity;
}
