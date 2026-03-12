namespace ContaCorrente.Domain.Entities;

public sealed class ContaCorrente
{
    public Guid IdContaCorrente { get; set; }
    public long Numero { get; set; }
    public string Cpf { get; set; } = string.Empty;
    public string Nome { get; set; } = string.Empty;
    public bool Ativo { get; set; } = true;
    public byte[] SenhaHash { get; set; } = [];
    public byte[] Salt { get; set; } = [];
}
