namespace TalentX.Application.Identity.Models;

public class IdentityResponse
{
    public bool Success { get; }
    public bool Succeeded => Success;
    public string[] Errors { get; }

    protected IdentityResponse(bool success, IEnumerable<string> errors)
    {
        Success = success;
        Errors = errors.ToArray();
    }

    public static IdentityResponse Ok() => new(true, Array.Empty<string>());

    public static IdentityResponse Fail(params string[] errors) => new(false, errors);

    public static IdentityResponse Fail(IEnumerable<string> errors) => new(false, errors);
}

public class IdentityResponse<T> : IdentityResponse
{
    public T? Data { get; }

    private IdentityResponse(bool success, T? data, IEnumerable<string> errors)
        : base(success, errors)
    {
        Data = data;
    }

    public static IdentityResponse<T> Ok(T data) => new(true, data, Array.Empty<string>());

    public new static IdentityResponse<T> Fail(params string[] errors) => new(false, default, errors);

    public new static IdentityResponse<T> Fail(IEnumerable<string> errors) => new(false, default, errors);
}