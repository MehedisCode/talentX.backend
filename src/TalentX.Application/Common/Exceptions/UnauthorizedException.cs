using System.Net;
namespace TalentX.Application.Common.Exceptions;
public class UnauthorizedException : AppException
{
    public UnauthorizedException(string message) : base(message) { }
    public override HttpStatusCode StatusCode => HttpStatusCode.Unauthorized;
}