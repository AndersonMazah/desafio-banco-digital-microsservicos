using ContaCorrente.Application.Common;
using ContaCorrente.Domain.Interfaces;
using MediatR;

namespace ContaCorrente.Application.Features.Usuario;

public sealed class InativarUsuarioHandler : IRequestHandler<InativarUsuarioCommand, ApplicationResult>
{
    private readonly IContaCorrenteRepository _contaRepository;
    private readonly IPasswordHasher _passwordHasher;

    public InativarUsuarioHandler(IContaCorrenteRepository contaRepository, IPasswordHasher passwordHasher)
    {
        _contaRepository = contaRepository;
        _passwordHasher = passwordHasher;
    }

    public async Task<ApplicationResult> Handle(InativarUsuarioCommand request, CancellationToken cancellationToken)
    {
        var senha = InputSanitizer.NormalizeSenha(request.Senha);
        if (!Validators.IsSenhaValida(senha))
        {
            return ApplicationResult.From(400, "SENHA inválida", ResponseTypes.TypeInvalidValue, null);
        }
        var conta = await _contaRepository.ObterPorIdAsync(request.IdContaCorrente, cancellationToken);
        if (conta is null)
        {
            return ApplicationResult.From(404, "Conta inválida", ResponseTypes.TypeInvalidAccount, null);
        }
        if (!conta.Ativo)
        {
            return ApplicationResult.From(409, "A conta já está inativa", ResponseTypes.TypeInvalidAccount, null);
        }
        if (!_passwordHasher.Verify(senha, conta.SenhaHash, conta.Salt))
        {
            return ApplicationResult.From(401, "Usuário não autorizado", ResponseTypes.TypeUserUnauthorized, null);
        }
        await _contaRepository.InativarAsync(conta.IdContaCorrente, cancellationToken);
        return ApplicationResult.NoContent();
    }

}
