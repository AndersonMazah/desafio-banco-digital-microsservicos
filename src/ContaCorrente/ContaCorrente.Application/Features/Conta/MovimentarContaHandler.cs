using System.Text.Json;
using ContaCorrente.Application.Common;
using ContaCorrente.Domain.Entities;
using ContaCorrente.Domain.Interfaces;
using MediatR;

namespace ContaCorrente.Application.Features.Conta;

public sealed class MovimentarContaHandler : IRequestHandler<MovimentarContaCommand, ApplicationResult>
{
    private readonly IContaCorrenteRepository _contaRepository;
    private readonly IMovimentoRepository _movimentoRepository;
    private readonly IIdempotenciaRepository _idempotenciaRepository;
    private readonly IUnitOfWork _unitOfWork;

    public MovimentarContaHandler(
        IContaCorrenteRepository contaRepository,
        IMovimentoRepository movimentoRepository,
        IIdempotenciaRepository idempotenciaRepository,
        IUnitOfWork unitOfWork)
    {
        _contaRepository = contaRepository;
        _movimentoRepository = movimentoRepository;
        _idempotenciaRepository = idempotenciaRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<ApplicationResult> Handle(MovimentarContaCommand request, CancellationToken cancellationToken)
    {
        if (request.Conta == Guid.Empty)
        {
            return ApplicationResult.From(400, "Conta inválida", ResponseTypes.TypeInvalidValue, null);
        }
        if (request.IdRequisicao == Guid.Empty)
        {
            return ApplicationResult.From(400, "ID Requisição inválido", ResponseTypes.TypeInvalidValue, null);
        }
        if (request.Valor <= 0)
        {
            return ApplicationResult.From(400, "Valor inválido", ResponseTypes.TypeInvalidValue, null);
        }
        var tipo = request.Tipo?.Trim().ToUpperInvariant() ?? string.Empty;
        if (!Validators.IsTipoMovimentoValido(tipo))
        {
            return ApplicationResult.From(400, "Tipo inválido", ResponseTypes.TypeInvalidValue, null);
        }
        var idempotenteExistente = await _idempotenciaRepository.ObterPorRequisicaoAsync(request.IdRequisicao, cancellationToken);
        if (idempotenteExistente is not null)
        {
            if (!idempotenteExistente.Status)
            {
                return ApplicationResult.From(409, "Requisição em processamento", ResponseTypes.TypeAlreadyExists, null);
            }
            var statusCode = int.TryParse(idempotenteExistente.StatusCode, out var parsedStatus) ? parsedStatus : 200;
            if (statusCode == 204)
            {
                return ApplicationResult.NoContent();
            }
            if (!string.IsNullOrWhiteSpace(idempotenteExistente.Resultado))
            {
                var envelope = JsonSerializer.Deserialize<ApiEnvelope>(idempotenteExistente.Resultado!);
                if (envelope is not null)
                {
                    return ApplicationResult.From(statusCode, envelope.Message, envelope.Type, envelope.Data);
                }
            }
            return ApplicationResult.From(statusCode, "Resultado idempotente", ResponseTypes.TypeSuccess, null);
        }
        await using var connection = await _unitOfWork.CreateOpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        var inserted = await _idempotenciaRepository.InserirEmAndamentoAsync(request.IdRequisicao, cancellationToken, connection, transaction);
        if (!inserted)
        {
            await transaction.RollbackAsync(cancellationToken);
            var concorrente = await _idempotenciaRepository.ObterPorRequisicaoAsync(request.IdRequisicao, cancellationToken);
            if (concorrente is not null && concorrente.Status)
            {
                var statusCode = int.TryParse(concorrente.StatusCode, out var parsedStatus) ? parsedStatus : 200;
                if (statusCode == 204)
                {
                    return ApplicationResult.NoContent();
                }
                if (!string.IsNullOrWhiteSpace(concorrente.Resultado))
                {
                    var envelope = JsonSerializer.Deserialize<ApiEnvelope>(concorrente.Resultado!);
                    if (envelope is not null)
                    {
                        return ApplicationResult.From(statusCode, envelope.Message, envelope.Type, envelope.Data);
                    }
                }
            }
            return ApplicationResult.From(409, "Requisição em processamento", ResponseTypes.TypeAlreadyExists, null);
        }

        async Task<ApplicationResult> FinalizarAsync(ApplicationResult result)
        {
            var statusCode = result.StatusCode.ToString();
            var json = result.Envelope is null ? null : JsonSerializer.Serialize(result.Envelope);
            await _idempotenciaRepository.AtualizarResultadoAsync(request.IdRequisicao, statusCode, json, cancellationToken, connection, transaction);
            await transaction.CommitAsync(cancellationToken);
            return result;
        }

        var conta = await _contaRepository.ObterPorIdAsync(request.Conta, cancellationToken, connection, transaction);
        if (conta is null)
        {
            return await FinalizarAsync(ApplicationResult.From(404, "Conta não localizada", ResponseTypes.TypeInvalidAccount, null));
        }
        if (!conta.Ativo)
        {
            return await FinalizarAsync(ApplicationResult.From(409, "Conta está inativa", ResponseTypes.TypeInactiveAccount, null));
        }
        if (request.IdContaJwt == request.Conta && tipo == "C")
        {
            return await FinalizarAsync(ApplicationResult.From(400, "Movimento de conta não permitido", ResponseTypes.TypeInvalidType, null));
        }
        var movimento = new Movimento
        {
            IdMovimento = Guid.NewGuid(),
            IdContaCorrente = request.Conta,
            DataMvto = DateTimeOffset.UtcNow,
            Tipo = tipo[0],
            Valor = request.Valor
        };
        await _movimentoRepository.InserirAsync(movimento, cancellationToken, connection, transaction);
        return await FinalizarAsync(ApplicationResult.NoContent());
    }

}
