using System;
using System.Collections.Generic;
using System.Linq;

namespace ManufacturingERP.Core;

public class PasswordHasherFactory
{
    private readonly IEnumerable<IPasswordHasher> _hashers;

    public PasswordHasherFactory(IEnumerable<IPasswordHasher> hashers)
    {
        _hashers = hashers;
    }

    /// <summary>
    /// Gets the hasher that supports the given hashed password format.
    /// </summary>
    public IPasswordHasher GetHasherForHash(string hashedPassword)
    {
        var hasher = _hashers.FirstOrDefault(h => h.SupportsHash(hashedPassword));
        if (hasher == null)
        {
            // Default to BCrypt if no prefix matches, or it might be raw plaintext 
            // from before any hashing implementation was added.
            return _hashers.First(h => h is BCryptHasher);
        }
        return hasher;
    }

    /// <summary>
    /// Gets the hasher by its algorithm name (from Admin settings).
    /// </summary>
    public IPasswordHasher GetHasherByName(string algorithmName)
    {
        var hasher = _hashers.FirstOrDefault(h => h.AlgorithmName.Equals(algorithmName, StringComparison.OrdinalIgnoreCase));
        if (hasher == null)
        {
            // Fallback to BCrypt as the most secure default
            return _hashers.First(h => h is BCryptHasher);
        }
        return hasher;
    }
}
