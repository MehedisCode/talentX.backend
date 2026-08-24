using System.Net;
namespace TalentX.Application.Common.Exceptions;
public class NotFoundException : AppException
{
    public NotFoundException(string message) : base(message) { }
    public override HttpStatusCode StatusCode => HttpStatusCode.NotFound;
}