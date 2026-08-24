using System.Net;
namespace TalentX.Application.Common.Exceptions;
public class ConflictException : AppException
{
    public ConflictException(string message) : base(message) { }
    public override HttpStatusCode StatusCode => HttpStatusCode.Conflict;
}