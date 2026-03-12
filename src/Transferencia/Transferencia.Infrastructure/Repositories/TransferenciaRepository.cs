using System.Data.Common;
using Dapper;
using Transferencia.Domain.Interfaces;
using TransferenciaEntity = Transferencia.Domain.Entities.Transferencia;

namespace Transferencia.Infrastructure.Repositories;

public sealed class TransferenciaRepository : ITransferenciaRepository
{
    public async Task InserirAsync(
        TransferenciaEntity transferencia,
        CancellationToken cancellationToken,
        DbConnection connection,
        DbTransaction transaction)
    {
        const string sql = @"
INSERT INTO transferencia (idtransferencia, idconta_origem, idconta_destino, datamvto, valor)
VALUES (@IdTransferencia, @IdContaOrigem, @IdContaDestino, @DataMvto, @Valor);";
        await connection.ExecuteAsync(
            new CommandDefinition(
                sql,
                new
                {
                    transferencia.IdTransferencia,
                    transferencia.IdContaOrigem,
                    transferencia.IdContaDestino,
                    transferencia.DataMvto,
                    transferencia.Valor
                },
                transaction,
                cancellationToken: cancellationToken));
    }

}
