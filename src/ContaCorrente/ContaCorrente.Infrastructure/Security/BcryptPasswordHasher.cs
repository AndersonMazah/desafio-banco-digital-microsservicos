using System.Security.Cryptography;
using System.Text;
using ContaCorrente.Domain.Interfaces;

namespace ContaCorrente.Infrastructure.Security;

public sealed class BcryptPasswordHasher : IPasswordHasher
{
    public (byte[] Hash, byte[] Salt) HashPassword(string senha)
    {
        var salt = RandomNumberGenerator.GetBytes(16);
        var salted = senha + Convert.ToBase64String(salt);
        var hash = BCrypt.Net.BCrypt.HashPassword(salted);
        return (Encoding.UTF8.GetBytes(hash), salt);
    }

    public bool Verify(string senha, byte[] hash, byte[] salt)
    {
        if (string.IsNullOrEmpty(senha) || hash.Length == 0)
        {
            return false;
        }
        var storedHash = NormalizeStoredHash(hash);
        if (string.IsNullOrEmpty(storedHash))
        {
            return false;
        }
        var saltBase64 = salt.Length > 0 ? Convert.ToBase64String(salt) : string.Empty;
        try
        {
            if (!string.IsNullOrEmpty(saltBase64) && BCrypt.Net.BCrypt.Verify(senha + saltBase64, storedHash))
            {
                return true;
            }
            return BCrypt.Net.BCrypt.Verify(senha, storedHash);
        }
        catch (Exception ex) when (ex is ArgumentException or FormatException or BCrypt.Net.SaltParseException)
        {
            return false;
        }
    }

    private static string NormalizeStoredHash(byte[] hashBytes)
    {
        var hashText = Encoding.UTF8.GetString(hashBytes).Trim().TrimEnd('\0');
        if (hashText.StartsWith("$2", StringComparison.Ordinal))
        {
            return hashText;
        }
        try
        {
            var decoded = Encoding.UTF8.GetString(Convert.FromBase64String(hashText)).Trim().TrimEnd('\0');
            return decoded.StartsWith("$2", StringComparison.Ordinal) ? decoded : string.Empty;
        }
        catch (FormatException)
        {
            return string.Empty;
        }
    }

}
