using ContaCorrente.Domain.Interfaces;
using Dapper;

namespace ContaCorrente.Infrastructure.Repositories;

public sealed class SaldoRepository : ISaldoRepository
{
    private readonly IUnitOfWork _unitOfWork;

    public SaldoRepository(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<decimal> ObterSaldoAsync(Guid idContaCorrente, CancellationToken cancellationToken)
    {
        const string sql = "SELECT saldo FROM vw_saldo_conta WHERE idcontacorrente = @IdContaCorrente";
        await using var connection = await _unitOfWork.CreateOpenConnectionAsync(cancellationToken);
        var saldo = await connection.ExecuteScalarAsync<decimal?>(new CommandDefinition(sql, new { IdContaCorrente = idContaCorrente }, cancellationToken: cancellationToken));
        return saldo ?? 0m;
    }

}
