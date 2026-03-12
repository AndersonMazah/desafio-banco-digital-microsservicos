using ContaCorrente.Domain.Entities;

namespace ContaCorrente.Tests.Common.Builders;

public sealed class ContaCorrenteBuilder
{
    private readonly ContaCorrente.Domain.Entities.ContaCorrente _conta = new()
    {
        IdContaCorrente = Guid.Parse("11111111-1111-1111-1111-111111111111"),
        Numero = 123456,
        Nome = "Anderson Silva",
        Cpf = "12345678909",
        Ativo = true,
        SenhaHash = [1, 2, 3],
        Salt = [4, 5, 6]
    };

    public ContaCorrenteBuilder ComId(Guid id)
    {
        _conta.IdContaCorrente = id;
        return this;
    }

    public ContaCorrenteBuilder ComNumero(long numero)
    {
        _conta.Numero = numero;
        return this;
    }

    public ContaCorrenteBuilder ComNome(string nome)
    {
        _conta.Nome = nome;
        return this;
    }

    public ContaCorrenteBuilder ComCpf(string cpf)
    {
        _conta.Cpf = cpf;
        return this;
    }

    public ContaCorrenteBuilder Ativa(bool ativo = true)
    {
        _conta.Ativo = ativo;
        return this;
    }

    public ContaCorrenteBuilder ComSenha(byte[] hash, byte[] salt)
    {
        _conta.SenhaHash = hash;
        _conta.Salt = salt;
        return this;
    }

    public ContaCorrente.Domain.Entities.ContaCorrente Build()
    {
        return new ContaCorrente.Domain.Entities.ContaCorrente
        {
            IdContaCorrente = _conta.IdContaCorrente,
            Numero = _conta.Numero,
            Nome = _conta.Nome,
            Cpf = _conta.Cpf,
            Ativo = _conta.Ativo,
            SenhaHash = [.. _conta.SenhaHash],
            Salt = [.. _conta.Salt]
        };
    }

}
