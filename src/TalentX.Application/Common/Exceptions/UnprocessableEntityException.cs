using System.Net;

namespace TalentX.Application.Common.Exceptions;

public class UnprocessableEntityException : AppException
{
    public UnprocessableEntityException(string message) : base(message) { }
    public override HttpStatusCode StatusCode => HttpStatusCode.UnprocessableEntity;
}
