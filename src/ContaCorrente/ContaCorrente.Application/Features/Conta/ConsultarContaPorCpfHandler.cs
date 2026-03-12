using ContaCorrente.Application.Common;
using ContaCorrente.Domain.Interfaces;
using MediatR;

namespace ContaCorrente.Application.Features.Conta;

public sealed class ConsultarContaPorCpfHandler : IRequestHandler<ConsultarContaPorCpfQuery, ApplicationResult>
{
    private readonly IContaCorrenteRepository _contaRepository;

    public ConsultarContaPorCpfHandler(IContaCorrenteRepository contaRepository)
    {
        _contaRepository = contaRepository;
    }

    public async Task<ApplicationResult> Handle(ConsultarContaPorCpfQuery request, CancellationToken cancellationToken)
    {
        var cpf = InputSanitizer.NormalizeCpf(request.Cpf);
        if (!Validators.IsCpfFormatoValido(cpf))
        {
            return ApplicationResult.From(400, "CPF inválido", ResponseTypes.TypeInvalidValue, null);
        }
        var conta = await _contaRepository.ObterPorCpfAsync(cpf, cancellationToken);
        if (conta is null || !conta.Ativo)
        {
            return ApplicationResult.From(404, "Conta não localizada", ResponseTypes.TypeInvalidAccount, null);
        }
        var primeiroNome = conta.Nome.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).FirstOrDefault() ?? conta.Nome;
        return ApplicationResult.From(200, "Consulta de Cliente", ResponseTypes.TypeSuccess, new
        {
            conta = conta.Numero,
            uuid = conta.IdContaCorrente,
            nome = primeiroNome,
            data_hora = DateTimeOffset.UtcNow
        });
    }

}
