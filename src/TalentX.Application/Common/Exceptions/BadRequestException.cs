using System.Net;
namespace TalentX.Application.Common.Exceptions;
public class BadRequestException : AppException
{
    public BadRequestException(string message) : base(message) { }
    public override HttpStatusCode StatusCode => HttpStatusCode.BadRequest;
}