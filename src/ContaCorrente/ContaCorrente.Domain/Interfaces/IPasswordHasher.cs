namespace ContaCorrente.Domain.Interfaces;

public interface IPasswordHasher
{
    (byte[] Hash, byte[] Salt) HashPassword(string senha);
    bool Verify(string senha, byte[] hash, byte[] salt);
}
