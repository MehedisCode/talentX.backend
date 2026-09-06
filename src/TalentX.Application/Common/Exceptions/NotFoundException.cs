using System.Net;

namespace TalentX.Application.Common.Exceptions;

public class NotFoundException : AppException
{
    public NotFoundException(string message, string? resourceId = null) : base(message, resourceId) { }
    public NotFoundException(string entityName, object key) : base($"{entityName} with ID '{key}' was not found.", key.ToString()) { }
    public override HttpStatusCode StatusCode => HttpStatusCode.NotFound;
}