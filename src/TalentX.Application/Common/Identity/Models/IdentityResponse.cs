namespace TalentX.Application.Common.Identity.Models;

public class IdentityResponse
{
    public bool Success { get; }
    public object? Data { get; }
    public string[] Errors { get; }

    private IdentityResponse(bool success, object? data, IEnumerable<string> errors)
    {
        Success = success;
        Data = data;
        Errors = errors.ToArray();
    }

    public static IdentityResponse Ok(object? data = null) =>
        new(true, data, Array.Empty<string>());

    public static IdentityResponse Fail(params string[] errors) =>
        new(false, null, errors);

    public static IdentityResponse Fail(IEnumerable<string> errors) =>
        new(false, null, errors);
}