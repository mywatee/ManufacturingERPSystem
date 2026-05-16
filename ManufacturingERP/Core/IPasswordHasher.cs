namespace ManufacturingERP.Core;

public interface IPasswordHasher
{
    string HashPassword(string password);
    bool VerifyPassword(string password, string hashedPassword);
    bool SupportsHash(string hashedPassword);
    string AlgorithmName { get; }
}
