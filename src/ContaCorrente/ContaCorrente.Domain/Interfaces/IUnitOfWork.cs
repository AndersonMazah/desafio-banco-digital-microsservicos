using System.Data.Common;

namespace ContaCorrente.Domain.Interfaces;

public interface IUnitOfWork
{
    Task<DbConnection> CreateOpenConnectionAsync(CancellationToken cancellationToken);
}
