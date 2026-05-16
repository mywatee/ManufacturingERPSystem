using Konscious.Security.Cryptography;
using System;
using System.Security.Cryptography;
using System.Text;

namespace ManufacturingERP.Core;

public class Argon2Hasher : IPasswordHasher
{
    private const int SaltSize = 16;
    private const int DegreeOfParallelism = 8;
    private const int Iterations = 4;
    private const int MemorySize = 1024 * 64; // 64 MB

    public string AlgorithmName => "Argon2";

    public string HashPassword(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var argon2 = new Argon2id(Encoding.UTF8.GetBytes(password))
        {
            Salt = salt,
            DegreeOfParallelism = DegreeOfParallelism,
            Iterations = Iterations,
            MemorySize = MemorySize
        };

        var hash = argon2.GetBytes(32);
        
        // Format: $argon2id$v=19$m=65536,t=4,p=8$salt$hash
        return $"$argon2id$v=19$m={MemorySize},t={Iterations},p={DegreeOfParallelism}${Convert.ToBase64String(salt)}${Convert.ToBase64String(hash)}";
    }

    public bool VerifyPassword(string password, string hashedPassword)
    {
        try
        {
            if (!SupportsHash(hashedPassword)) return false;

            var parts = hashedPassword.Split('$');
            // parts[0] is empty, parts[1] is argon2id, parts[2] is v=19, parts[3] is m=..., parts[4] is salt, parts[5] is hash
            var salt = Convert.FromBase64String(parts[4]);
            var expectedHash = parts[5];

            var argon2 = new Argon2id(Encoding.UTF8.GetBytes(password))
            {
                Salt = salt,
                DegreeOfParallelism = DegreeOfParallelism,
                Iterations = Iterations,
                MemorySize = MemorySize
            };

            var verifiedHash = Convert.ToBase64String(argon2.GetBytes(32));
            return verifiedHash == expectedHash;
        }
        catch
        {
            return false;
        }
    }

    public bool SupportsHash(string hashedPassword)
    {
        return hashedPassword.StartsWith("$argon2id$");
    }
}
