using System.Security.Cryptography;

namespace TalentX.Application.Common.Security;

/// <summary>
/// Generates cryptographically strong temporary passwords that satisfy the ASP.NET
/// Core Identity policy (minimum length 8, at least one uppercase, one lowercase,
/// one digit and one non-alphanumeric character).
/// </summary>
public static class TemporaryPasswordGenerator
{
    private const string Lowercase = "abcdefghijkmnpqrstuvwxyz"; // excludes 'l'
    private const string Uppercase = "ABCDEFGHJKLMNPQRSTUVWXYZ"; // excludes 'I', 'O'
    private const string Digits = "23456789";                     // excludes '0', '1'
    private const string Specials = "!@#$%^&*-_=+";

    private static readonly string All =
        Lowercase + Uppercase + Digits + Specials;

    /// <summary>
    /// Generates a temporary password of the requested length. Each generated
    /// password is guaranteed to contain at least one character from every
    /// required class, then shuffled so classes are not clustered.
    /// </summary>
    /// <param name="length">Password length. Must be at least 8.</param>
    public static string Generate(int length = 12)
    {
        if (length < 8)
        {
            throw new ArgumentOutOfRangeException(nameof(length), "Temporary password length must be at least 8.");
        }

        var password = new char[length];
        password[0] = RandomPick(Lowercase);
        password[1] = RandomPick(Uppercase);
        password[2] = RandomPick(Digits);
        password[3] = RandomPick(Specials);

        for (var i = 4; i < length; i++)
        {
            password[i] = RandomPick(All);
        }

        Shuffle(password);

        return new string(password);
    }

    private static char RandomPick(string pool) =>
        pool[RandomNumberGenerator.GetInt32(pool.Length)];

    private static void Shuffle(char[] chars)
    {
        for (var i = chars.Length - 1; i > 0; i--)
        {
            var j = RandomNumberGenerator.GetInt32(i + 1);
            (chars[i], chars[j]) = (chars[j], chars[i]);
        }
    }
}