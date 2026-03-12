namespace Transferencia.Domain.Entities;

public sealed class Transferencia
{
    public Guid IdTransferencia { get; set; }
    public Guid IdContaOrigem { get; set; }
    public Guid IdContaDestino { get; set; }
    public DateTimeOffset DataMvto { get; set; }
    public decimal Valor { get; set; }
}
