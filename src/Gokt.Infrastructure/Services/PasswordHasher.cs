using System.Security.Cryptography;
using System.Text;
using Gokt.Application.Interfaces;
using Konscious.Security.Cryptography;

namespace Gokt.Infrastructure.Services;

public class PasswordHasher : IPasswordHasher
{
    // OWASP 2024 recommended Argon2id parameters
    private const int DegreeOfParallelism = 1;
    private const int Iterations = 2;
    private const int MemorySize = 19456; // ~19 MB
    private const int HashLength = 32;
    private const int SaltLength = 16;

    public string Hash(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltLength);
        var hash = ComputeArgon2id(password, salt);

        return $"$argon2id$v=19$m={MemorySize},t={Iterations},p={DegreeOfParallelism}" +
               $"${Convert.ToBase64String(salt)}${Convert.ToBase64String(hash)}";
    }

    public bool Verify(string password, string storedHash)
    {
        try
        {
            var parts = storedHash.Split('$', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 5 || parts[0] != "argon2id")
                return false;

            var salt = Convert.FromBase64String(parts[3]);
            var expectedHash = Convert.FromBase64String(parts[4]);
            var actualHash = ComputeArgon2id(password, salt);

            // Constant-time comparison prevents timing attacks
            return CryptographicOperations.FixedTimeEquals(actualHash, expectedHash);
        }
        catch
        {
            return false;
        }
    }

    private static byte[] ComputeArgon2id(string password, byte[] salt)
    {
        using var argon2 = new Argon2id(Encoding.UTF8.GetBytes(password))
        {
            Salt = salt,
            DegreeOfParallelism = DegreeOfParallelism,
            Iterations = Iterations,
            MemorySize = MemorySize
        };
        return argon2.GetBytes(HashLength);
    }
}
