namespace ContaCorrente.Domain.Entities;

public sealed class Idempotencia
{
    public Guid IdIdempotencia { get; set; }
    public Guid Requisicao { get; set; }
    public bool Status { get; set; }
    public string StatusCode { get; set; } = "202";
    public string? Resultado { get; set; }
}
