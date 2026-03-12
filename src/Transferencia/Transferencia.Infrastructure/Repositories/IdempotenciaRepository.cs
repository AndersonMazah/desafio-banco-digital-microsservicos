using System.Data.Common;
using Transferencia.Domain.Entities;
using Transferencia.Domain.Interfaces;
using Transferencia.Infrastructure.Dpo;
using Dapper;
using Npgsql;

namespace Transferencia.Infrastructure.Repositories;

public sealed class IdempotenciaRepository : IIdempotenciaRepository
{
    private readonly IUnitOfWork _unitOfWork;

    public IdempotenciaRepository(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Idempotencia?> ObterPorRequisicaoAsync(Guid requisicao, CancellationToken cancellationToken)
    {
        const string sql = @"
SELECT ididempotencia AS IdIdempotencia, requisicao AS Requisicao, status AS Status, status_code AS StatusCode, resultado AS Resultado
FROM idempotencia
WHERE requisicao = @Requisicao";
        await using var connection = await _unitOfWork.CreateOpenConnectionAsync(cancellationToken);
        var dpo = await connection.QuerySingleOrDefaultAsync<IdempotenciaDpo>(new CommandDefinition(sql, new { Requisicao = requisicao }, cancellationToken: cancellationToken));
        return dpo is null ? null : Map(dpo);
    }

    public async Task<bool> InserirEmAndamentoAsync(Guid requisicao, CancellationToken cancellationToken)
    {
        const string sql = @"
INSERT INTO idempotencia (ididempotencia, requisicao, status, status_code, resultado)
VALUES (@IdIdempotencia, @Requisicao, FALSE, '202', NULL);";
        await using var connection = await _unitOfWork.CreateOpenConnectionAsync(cancellationToken);
        try
        {
            await connection.ExecuteAsync(
                new CommandDefinition(
                    sql,
                    new
                    {
                        IdIdempotencia = Guid.NewGuid(),
                        Requisicao = requisicao
                    },
                    cancellationToken: cancellationToken));

            return true;
        }
        catch (PostgresException ex) when (ex.SqlState == "23505")
        {
            return false;
        }
    }

    public async Task AtualizarResultadoAsync(
        Guid requisicao,
        bool status,
        string statusCode,
        string? resultado,
        CancellationToken cancellationToken,
        DbConnection? connection = null,
        DbTransaction? transaction = null)
    {
        const string sql = @"
UPDATE idempotencia
SET status = @Status,
    status_code = @StatusCode,
    resultado = @Resultado
WHERE requisicao = @Requisicao;";
        if (connection is not null)
        {
            await connection.ExecuteAsync(
                new CommandDefinition(
                    sql,
                    new
                    {
                        Requisicao = requisicao,
                        Status = status,
                        StatusCode = statusCode,
                        Resultado = resultado
                    },
                    transaction,
                    cancellationToken: cancellationToken));
            return;
        }
        await using var localConnection = await _unitOfWork.CreateOpenConnectionAsync(cancellationToken);
        await localConnection.ExecuteAsync(
            new CommandDefinition(
                sql,
                new
                {
                    Requisicao = requisicao,
                    Status = status,
                    StatusCode = statusCode,
                    Resultado = resultado
                },
                cancellationToken: cancellationToken));
    }

    private static Idempotencia Map(IdempotenciaDpo dpo)
    {
        return new()
        {
            IdIdempotencia = dpo.IdIdempotencia,
            Requisicao = dpo.Requisicao,
            Status = dpo.Status,
            StatusCode = dpo.StatusCode,
            Resultado = dpo.Resultado
        };
    }

}
