namespace TalentX.Application.Common.Exceptions;
public class ForbiddenException : Exception
{
    public ForbiddenException() : base("You are not allowed to perform this action."){}
}