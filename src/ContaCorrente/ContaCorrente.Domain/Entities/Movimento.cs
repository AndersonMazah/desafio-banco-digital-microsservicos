namespace ContaCorrente.Domain.Entities;

public sealed class Movimento
{
    public Guid IdMovimento { get; set; }
    public Guid IdContaCorrente { get; set; }
    public DateTimeOffset DataMvto { get; set; }
    public char Tipo { get; set; }
    public decimal Valor { get; set; }
}
