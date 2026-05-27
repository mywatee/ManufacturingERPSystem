using System.Security.Cryptography;

namespace ManufacturingERP.Core;

public static class PasswordGenerator
{
    private const string Lower = "abcdefghijklmnopqrstuvwxyz";
    private const string Upper = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
    private const string Digits = "0123456789";
    private const string Specials = "!@#$%^&*_-+=";

    public static string GenerateTemporaryPassword(int length = 12)
    {
        if (length < 8) length = 8;

        Span<char> password = length <= 128 ? stackalloc char[length] : new char[length];
        password[0] = Pick(Lower);
        password[1] = Pick(Upper);
        password[2] = Pick(Digits);
        password[3] = Pick(Specials);

        var all = Lower + Upper + Digits + Specials;
        for (int i = 4; i < length; i++)
        {
            password[i] = Pick(all);
        }

        Shuffle(password);
        return new string(password);
    }

    private static char Pick(string alphabet)
    {
        var idx = RandomNumberGenerator.GetInt32(alphabet.Length);
        return alphabet[idx];
    }

    private static void Shuffle(Span<char> buffer)
    {
        for (int i = buffer.Length - 1; i > 0; i--)
        {
            int j = RandomNumberGenerator.GetInt32(i + 1);
            (buffer[i], buffer[j]) = (buffer[j], buffer[i]);
        }
    }
}

