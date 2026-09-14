namespace TalentX.Application.Common.Constants;

/// <summary>
/// Purpose-qualified keys for the key-scoped <see cref="IOtpService"/>, preventing
/// OTP codes issued for one flow from satisfying another.
/// </summary>
public static class OtpKeys
{
    /// <summary>
    /// Key for the account-deletion verification OTP.
    /// </summary>
    public static string DeleteAccount(string email) => $"delete-account:{email.ToLowerInvariant()}";
}
