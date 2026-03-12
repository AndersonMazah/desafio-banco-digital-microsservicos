namespace Transferencia.Domain.Models;

public sealed class ContaCorrenteOperationResult
{
    public int StatusCode { get; set; }
    public string? Message { get; set; }
    public string? Type { get; set; }
    public object? Data { get; set; }
}
