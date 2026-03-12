namespace ContaCorrente.Domain.Interfaces;

public interface ISaldoRepository
{
    Task<decimal> ObterSaldoAsync(Guid idContaCorrente, CancellationToken cancellationToken);
}
