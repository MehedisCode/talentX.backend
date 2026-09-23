namespace TalentX.Application.Common.Constants;

public static class OtpKeys
{
    public static string DeleteAccount(string email) => $"delete-account:{email.ToLowerInvariant()}";
}
