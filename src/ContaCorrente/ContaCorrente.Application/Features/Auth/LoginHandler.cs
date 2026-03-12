using ContaCorrente.Application.Common;
using ContaCorrente.Domain.Interfaces;
using MediatR;

namespace ContaCorrente.Application.Features.Auth;

public sealed class LoginHandler : IRequestHandler<LoginCommand, ApplicationResult>
{
    private readonly IContaCorrenteRepository _contaRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IAuthTokenService _authTokenService;

    public LoginHandler(IContaCorrenteRepository contaRepository, IPasswordHasher passwordHasher, IAuthTokenService authTokenService)
    {
        _contaRepository = contaRepository;
        _passwordHasher = passwordHasher;
        _authTokenService = authTokenService;
    }

    public async Task<ApplicationResult> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        if (!Validators.IsContaLoginValida(request.Conta))
        {
            return ApplicationResult.From(400, "Conta inválida", ResponseTypes.TypeInvalidValue, null);
        }
        var cpf = string.IsNullOrWhiteSpace(request.Cpf) ? string.Empty : InputSanitizer.NormalizeCpf(request.Cpf);
        if (!string.IsNullOrWhiteSpace(request.Cpf) && !Validators.IsCpfFormatoValido(cpf))
        {
            return ApplicationResult.From(400, "CPF inválido", ResponseTypes.TypeInvalidValue, null);
        }
        var contaInformada = !string.IsNullOrWhiteSpace(request.Conta);
        var cpfInformado = !string.IsNullOrWhiteSpace(cpf);
        if (!contaInformada && !cpfInformado)
        {
            return ApplicationResult.From(400, "É necessário informar a Conta ou o CPF", ResponseTypes.TypeInvalidValue, null);
        }
        var senha = InputSanitizer.NormalizeSenha(request.Senha);
        if (!Validators.IsSenhaValida(senha))
        {
            return ApplicationResult.From(400, "SENHA inválida", ResponseTypes.TypeInvalidValue, null);
        }
        long? contaNumero = null;
        if (contaInformada)
        {
            contaNumero = long.Parse(request.Conta!.Trim());
        }
        var conta = await _contaRepository.ObterPorContaOuCpfAsync(contaNumero, cpfInformado ? cpf : null, cancellationToken);
        if (conta is null)
        {
            return ApplicationResult.From(404, "Usuário não autorizado", ResponseTypes.TypeUserUnauthorized, null);
        }
        if (!_passwordHasher.Verify(senha, conta.SenhaHash, conta.Salt))
        {
            return ApplicationResult.From(401, "Usuário não autorizado", ResponseTypes.TypeUserUnauthorized, null);
        }
        if (!conta.Ativo)
        {
            return ApplicationResult.From(401, "Usuário não autorizado", ResponseTypes.TypeUserUnauthorized, null);
        }
        var token = _authTokenService.GenerateToken(conta.IdContaCorrente);
        return ApplicationResult.From(200, "Usuário autenticado", ResponseTypes.TypeUserAuthorized, token);
    }
}
