using System.Data.Common;
using Transferencia.Domain.Entities;

namespace Transferencia.Domain.Interfaces;

public interface IContaCorrenteRepository
{
    Task<ContaCorrente?> ObterPorIdAsync(
        Guid idContaCorrente,
        CancellationToken cancellationToken,
        DbConnection? connection = null,
        DbTransaction? transaction = null);
}
