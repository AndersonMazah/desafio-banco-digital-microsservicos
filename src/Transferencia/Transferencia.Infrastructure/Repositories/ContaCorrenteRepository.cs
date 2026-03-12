using System.Data.Common;
using Transferencia.Domain.Interfaces;
using Transferencia.Infrastructure.Dpo;
using Dapper;
using Transferencia.Domain.Entities;

namespace Transferencia.Infrastructure.Repositories;

public sealed class ContaCorrenteRepository : IContaCorrenteRepository
{
    private readonly IUnitOfWork _unitOfWork;

    public ContaCorrenteRepository(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<ContaCorrente?> ObterPorIdAsync(
        Guid idContaCorrente,
        CancellationToken cancellationToken,
        DbConnection? connection = null,
        DbTransaction? transaction = null)
    {
        const string sql = @"
SELECT idcontacorrente AS IdContaCorrente, ativo AS Ativo
FROM contacorrente
WHERE idcontacorrente = @IdContaCorrente";
        if (connection is not null)
        {
            var dpoShared = await connection.QuerySingleOrDefaultAsync<ContaCorrenteDpo>(
                new CommandDefinition(sql, new { IdContaCorrente = idContaCorrente }, transaction, cancellationToken: cancellationToken));
            return dpoShared is null ? null : Map(dpoShared);
        }
        await using var localConnection = await _unitOfWork.CreateOpenConnectionAsync(cancellationToken);
        var dpo = await localConnection.QuerySingleOrDefaultAsync<ContaCorrenteDpo>(
            new CommandDefinition(sql, new { IdContaCorrente = idContaCorrente }, cancellationToken: cancellationToken));
        return dpo is null ? null : Map(dpo);
    }

    private static ContaCorrente Map(ContaCorrenteDpo dpo)
    {
        return new()
        {
            IdContaCorrente = dpo.IdContaCorrente,
            Ativo = dpo.Ativo
        };
    }

}
