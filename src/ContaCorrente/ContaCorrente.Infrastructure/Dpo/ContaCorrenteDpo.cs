namespace ContaCorrente.Infrastructure.Dpo;

public sealed class ContaCorrenteDpo
{
    public Guid IdContaCorrente { get; set; }
    public long Numero { get; set; }
    public string Cpf { get; set; } = string.Empty;
    public string Nome { get; set; } = string.Empty;
    public bool Ativo { get; set; }
    public byte[] SenhaHash { get; set; } = [];
    public byte[] Salt { get; set; } = [];
}
