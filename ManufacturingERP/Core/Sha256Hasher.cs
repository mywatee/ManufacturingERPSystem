using System;
using System.Security.Cryptography;
using System.Text;

namespace ManufacturingERP.Core;

public class Sha256Hasher : IPasswordHasher
{
    private const string Prefix = "$sha256$";

    public string AlgorithmName => "SHA-256";

    public string HashPassword(string password)
    {
        using var sha256 = SHA256.Create();
        var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
        return Prefix + Convert.ToBase64String(bytes);
    }

    public bool VerifyPassword(string password, string hashedPassword)
    {
        if (!SupportsHash(hashedPassword)) return false;

        string actualHash = hashedPassword.Substring(Prefix.Length);
        using var sha256 = SHA256.Create();
        var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
        return Convert.ToBase64String(bytes) == actualHash;
    }

    public bool SupportsHash(string hashedPassword)
    {
        return hashedPassword.StartsWith(Prefix);
    }
}
