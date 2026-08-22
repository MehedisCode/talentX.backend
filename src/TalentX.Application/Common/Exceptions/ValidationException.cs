using System.Net;

namespace TalentX.Application.Common.Exceptions;

public class ValidationException : AppException
{
    public ValidationException(string message) : base(message) { }
    public override HttpStatusCode StatusCode => HttpStatusCode.BadRequest;
}
