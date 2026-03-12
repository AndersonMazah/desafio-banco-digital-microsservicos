using ContaCorrente.Application.Common;
using ContaCorrente.Domain.Interfaces;
using MediatR;
using ContaCorrenteEntity = ContaCorrente.Domain.Entities.ContaCorrente;

namespace ContaCorrente.Application.Features.Usuario;

public sealed class CadastrarUsuarioHandler : IRequestHandler<CadastrarUsuarioCommand, ApplicationResult>
{
    private readonly IContaCorrenteRepository _contaRepository;
    private readonly IPasswordHasher _passwordHasher;

    public CadastrarUsuarioHandler(IContaCorrenteRepository contaRepository, IPasswordHasher passwordHasher)
    {
        _contaRepository = contaRepository;
        _passwordHasher = passwordHasher;
    }

    public async Task<ApplicationResult> Handle(CadastrarUsuarioCommand request, CancellationToken cancellationToken)
    {
        var nome = InputSanitizer.NormalizeName(request.Nome);
        if (!Validators.IsNomeValido(nome))
        {
            return ApplicationResult.From(400, "Nome inválido", ResponseTypes.TypeInvalidValue, null);
        }
        var cpf = InputSanitizer.NormalizeCpf(request.Cpf);
        if (!Validators.IsCpfFormatoValido(cpf) || !Validators.IsCpfValido(cpf))
        {
            return ApplicationResult.From(400, "CPF inválido", ResponseTypes.TypeInvalidDocument, null);
        }
        var senha = InputSanitizer.NormalizeSenha(request.Senha);
        if (!Validators.IsSenhaValida(senha))
        {
            return ApplicationResult.From(400, "Formato da SENHA está inválido. Formato aceito: Seis números", ResponseTypes.TypeInvalidValue, null);
        }
        if (await _contaRepository.CpfExisteAsync(cpf, cancellationToken))
        {
            return ApplicationResult.From(409, "CPF já cadastrado", ResponseTypes.TypeAlreadyExists, null);
        }
        var (hash, salt) = _passwordHasher.HashPassword(senha);
        var conta = new ContaCorrenteEntity
        {
            IdContaCorrente = Guid.NewGuid(),
            Nome = nome,
            Cpf = cpf,
            SenhaHash = hash,
            Salt = salt,
            Ativo = true
        };
        var numeroConta = await _contaRepository.CriarAsync(conta, cancellationToken);
        return ApplicationResult.From(201, "Usuário cadastrado", ResponseTypes.TypeSuccess, new { conta = numeroConta });
    }

}
