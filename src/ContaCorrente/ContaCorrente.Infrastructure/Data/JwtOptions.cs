namespace ContaCorrente.Infrastructure.Data;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";
    public string Issuer { get; set; } = "contacorrente-api";
    public string Audience { get; set; } = "contacorrente-client";
    public string Key { get; set; } = "hJ28actfNTkqYxBEkIlzzwE1P307205E";
    public int ExpirationMinutes { get; set; } = 60;
}
