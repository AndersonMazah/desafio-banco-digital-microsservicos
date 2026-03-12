namespace Transferencia.Infrastructure.Data;

public sealed class ContaCorrenteApiOptions
{
    public const string SectionName = "ContaCorrenteApi";

    public string BaseUrl { get; set; } = string.Empty;
}
