using System.Net;
namespace TalentX.Application.Common.Exceptions;
public class ForbiddenException : AppException
{
    public ForbiddenException(string message) : base(message) { }
    public override HttpStatusCode StatusCode => HttpStatusCode.Forbidden;
}