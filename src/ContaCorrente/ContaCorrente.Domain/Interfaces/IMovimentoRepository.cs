using System.Data.Common;
using ContaCorrente.Domain.Entities;

namespace ContaCorrente.Domain.Interfaces;

public interface IMovimentoRepository
{
    Task InserirAsync(Movimento movimento, CancellationToken cancellationToken, DbConnection connection, DbTransaction transaction);
}
