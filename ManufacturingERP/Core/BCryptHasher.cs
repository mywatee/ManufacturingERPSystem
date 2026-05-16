using BCrypt.Net;

namespace ManufacturingERP.Core;

public class BCryptHasher : IPasswordHasher
{
    public string AlgorithmName => "bcrypt (Khuyến nghị)";

    public string HashPassword(string password)
    {
        return BCrypt.Net.BCrypt.HashPassword(password);
    }

    public bool VerifyPassword(string password, string hashedPassword)
    {
        try 
        {
            return BCrypt.Net.BCrypt.Verify(password, hashedPassword);
        }
        catch
        {
            return false;
        }
    }

    public bool SupportsHash(string hashedPassword)
    {
        // BCrypt hashes usually start with $2a$, $2b$, or $2y$
        return hashedPassword.StartsWith("$2a$") || 
               hashedPassword.StartsWith("$2b$") || 
               hashedPassword.StartsWith("$2y$");
    }
}
