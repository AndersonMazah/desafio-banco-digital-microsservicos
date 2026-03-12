using System.Data.Common;

namespace Transferencia.Domain.Interfaces;

public interface IUnitOfWork
{
    Task<DbConnection> CreateOpenConnectionAsync(CancellationToken cancellationToken);
}
