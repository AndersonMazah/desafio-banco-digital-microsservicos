using System.Data.Common;
using ContaCorrenteEntity = ContaCorrente.Domain.Entities.ContaCorrente;

namespace ContaCorrente.Domain.Interfaces;

public interface IContaCorrenteRepository
{
    Task<bool> CpfExisteAsync(string cpf, CancellationToken cancellationToken);
    Task<long> CriarAsync(ContaCorrenteEntity conta, CancellationToken cancellationToken);
    Task<ContaCorrenteEntity?> ObterPorIdAsync(Guid idContaCorrente, CancellationToken cancellationToken, DbConnection? connection = null, DbTransaction? transaction = null);
    Task<ContaCorrenteEntity?> ObterPorContaOuCpfAsync(long? conta, string? cpf, CancellationToken cancellationToken);
    Task<ContaCorrenteEntity?> ObterPorCpfAsync(string cpf, CancellationToken cancellationToken);
    Task InativarAsync(Guid idContaCorrente, CancellationToken cancellationToken);
}
