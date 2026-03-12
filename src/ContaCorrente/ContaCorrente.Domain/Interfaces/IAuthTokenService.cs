namespace ContaCorrente.Domain.Interfaces;

public interface IAuthTokenService
{
    string GenerateToken(Guid idContaCorrente);
}
