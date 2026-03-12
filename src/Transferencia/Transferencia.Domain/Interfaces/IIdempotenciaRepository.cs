using System.Data.Common;
using Transferencia.Domain.Entities;

namespace Transferencia.Domain.Interfaces;

public interface IIdempotenciaRepository
{
    Task<Idempotencia?> ObterPorRequisicaoAsync(Guid requisicao, CancellationToken cancellationToken);
    Task<bool> InserirEmAndamentoAsync(Guid requisicao, CancellationToken cancellationToken);
    Task AtualizarResultadoAsync(
        Guid requisicao,
        bool status,
        string statusCode,
        string? resultado,
        CancellationToken cancellationToken,
        DbConnection? connection = null,
        DbTransaction? transaction = null);
}
