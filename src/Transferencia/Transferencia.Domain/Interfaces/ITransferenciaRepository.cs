using System.Data.Common;
using TransferenciaEntity = Transferencia.Domain.Entities.Transferencia;

namespace Transferencia.Domain.Interfaces;

public interface ITransferenciaRepository
{
    Task InserirAsync(
        TransferenciaEntity transferencia,
        CancellationToken cancellationToken,
        DbConnection connection,
        DbTransaction transaction);
}
