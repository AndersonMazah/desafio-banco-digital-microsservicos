namespace Transferencia.Infrastructure.Data;

public sealed class PostgresOptions
{
    public const string SectionName = "ConnectionStrings";
    public string DefaultConnection { get; set; } = string.Empty;
}
