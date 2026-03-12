using System.Data.Common;
using ContaCorrente.Domain.Entities;

namespace ContaCorrente.Domain.Interfaces;

public interface IIdempotenciaRepository
{
    Task<Idempotencia?> ObterPorRequisicaoAsync(Guid requisicao, CancellationToken cancellationToken);
    Task<bool> InserirEmAndamentoAsync(Guid requisicao, CancellationToken cancellationToken, DbConnection connection, DbTransaction transaction);
    Task AtualizarResultadoAsync(Guid requisicao, string statusCode, string? resultado, CancellationToken cancellationToken, DbConnection connection, DbTransaction transaction);
}
