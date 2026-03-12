namespace Transferencia.Infrastructure.Data;

public sealed class ContaCorrenteApiOptions
{
    public const string SectionName = "ContaCorrenteApi";
    public const string ServicesSectionName = "Services";
    public const string ServicesBaseUrlKey = "Services:ContaCorrenteBaseUrl";

    public string BaseUrl { get; set; } = string.Empty;
}
