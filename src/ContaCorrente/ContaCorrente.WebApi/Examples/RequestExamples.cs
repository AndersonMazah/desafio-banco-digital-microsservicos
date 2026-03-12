using ContaCorrente.WebApi.Contracts;
using Swashbuckle.AspNetCore.Filters;

namespace ContaCorrente.WebApi.Examples;

public sealed class CadastrarUsuarioRequestExample : IExamplesProvider<CadastrarUsuarioRequest>
{
    public CadastrarUsuarioRequest GetExamples()
    {
        return new() { Nome = "Anderson", Cpf = "123.456.789-09", Senha = "123456" };
    }
}

public sealed class InativarUsuarioRequestExample : IExamplesProvider<InativarUsuarioRequest>
{
    public InativarUsuarioRequest GetExamples()
    {
        return new() { Senha = "123456" };
    }
}

public sealed class LoginRequestExample : IExamplesProvider<LoginRequest>
{
    public LoginRequest GetExamples()
    {
        return new() { Conta = "1", Senha = "123456" };
    }
}

public sealed class MovimentarContaRequestExample : IExamplesProvider<MovimentarContaRequest>
{
    public MovimentarContaRequest GetExamples()
    {
        return new()
        {
            Conta = Guid.Parse("d2719c6b-1fcf-49d8-98b6-08cb1ca7ef01"),
            IdRequisicao = Guid.Parse("7834f293-dfd5-4d27-b3dc-11c4c6f4ac31"),
            Valor = 150.75m,
            Tipo = "D"
        };
    }
}

public sealed class ConsultarContaRequestExample : IExamplesProvider<ConsultarContaRequest>
{
    public ConsultarContaRequest GetExamples()
    {
        return new() { Cpf = "12345678909" };
    }
}
