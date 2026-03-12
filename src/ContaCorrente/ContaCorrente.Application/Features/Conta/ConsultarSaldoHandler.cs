using ContaCorrente.Application.Common;
using ContaCorrente.Domain.Interfaces;
using MediatR;

namespace ContaCorrente.Application.Features.Conta;

public sealed class ConsultarSaldoHandler : IRequestHandler<ConsultarSaldoQuery, ApplicationResult>
{
    private readonly IContaCorrenteRepository _contaRepository;
    private readonly ISaldoRepository _saldoRepository;

    public ConsultarSaldoHandler(IContaCorrenteRepository contaRepository, ISaldoRepository saldoRepository)
    {
        _contaRepository = contaRepository;
        _saldoRepository = saldoRepository;
    }

    public async Task<ApplicationResult> Handle(ConsultarSaldoQuery request, CancellationToken cancellationToken)
    {
        var conta = await _contaRepository.ObterPorIdAsync(request.IdContaJwt, cancellationToken);
        if (conta is null)
        {
            return ApplicationResult.From(404, "Conta não localizada", ResponseTypes.TypeInvalidAccount, null);
        }
        if (!conta.Ativo)
        {
            return ApplicationResult.From(409, "Conta está inativa", ResponseTypes.TypeInactiveAccount, null);
        }
        var saldo = await _saldoRepository.ObterSaldoAsync(conta.IdContaCorrente, cancellationToken);
        return ApplicationResult.From(200, "Consulta de saldo", ResponseTypes.TypeSuccess, new
        {
            conta = conta.Numero,
            nome = conta.Nome,
            data_hora = DateTimeOffset.UtcNow,
            saldo
        });
    }

}
