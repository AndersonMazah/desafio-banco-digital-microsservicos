namespace ContaCorrente.Domain.Models;

public sealed class ContaSaldo
{
    public Guid IdContaCorrente { get; set; }
    public decimal Saldo { get; set; }
}
