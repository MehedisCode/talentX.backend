using System.Net;

namespace TalentX.Application.Common.Exceptions;
public abstract class AppException : Exception
{
    protected AppException(string message) : base(message) { }
    public abstract HttpStatusCode StatusCode { get; }
}

