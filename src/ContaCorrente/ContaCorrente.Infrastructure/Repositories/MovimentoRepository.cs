using System.Data.Common;
using ContaCorrente.Domain.Entities;
using ContaCorrente.Domain.Interfaces;
using Dapper;

namespace ContaCorrente.Infrastructure.Repositories;

public sealed class MovimentoRepository : IMovimentoRepository
{
    public async Task InserirAsync(Movimento movimento, CancellationToken cancellationToken, DbConnection connection, DbTransaction transaction)
    {
        const string sql = @"
INSERT INTO movimento (idmovimento, idcontacorrente, datamvto, tipo, valor)
VALUES (@IdMovimento, @IdContaCorrente, @DataMvto, @Tipo, @Valor);";
        await connection.ExecuteAsync(new CommandDefinition(sql, new
        {
            movimento.IdMovimento,
            movimento.IdContaCorrente,
            movimento.DataMvto,
            movimento.Tipo,
            movimento.Valor
        }, transaction, cancellationToken: cancellationToken));
    }

}
