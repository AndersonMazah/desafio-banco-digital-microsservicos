using Transferencia.Domain.Models;

namespace Transferencia.Domain.Interfaces;

public interface IContaCorrenteClient
{
    Task<ContaCorrenteOperationResult> MovimentarAsync(Guid conta, decimal valor, string tipo, CancellationToken cancellationToken);
}
