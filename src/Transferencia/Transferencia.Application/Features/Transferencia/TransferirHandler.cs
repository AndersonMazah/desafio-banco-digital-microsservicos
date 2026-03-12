using System.Diagnostics;
using System.Text.Json;
using MediatR;
using Transferencia.Application.Common;
using Transferencia.Domain.Interfaces;
using Transferencia.Domain.Models;
using TransferenciaEntity = Transferencia.Domain.Entities.Transferencia;

namespace Transferencia.Application.Features.Transferencia;

public sealed class TransferirHandler : IRequestHandler<TransferirCommand, ApplicationResult>
{
    private readonly IContaCorrenteRepository _contaCorrenteRepository;
    private readonly IContaCorrenteClient _contaCorrenteClient;
    private readonly IIdempotenciaRepository _idempotenciaRepository;
    private readonly ITransferenciaRepository _transferenciaRepository;
    private readonly IUnitOfWork _unitOfWork;

    public TransferirHandler(
        IContaCorrenteRepository contaCorrenteRepository,
        IContaCorrenteClient contaCorrenteClient,
        IIdempotenciaRepository idempotenciaRepository,
        ITransferenciaRepository transferenciaRepository,
        IUnitOfWork unitOfWork)
    {
        _contaCorrenteRepository = contaCorrenteRepository;
        _contaCorrenteClient = contaCorrenteClient;
        _idempotenciaRepository = idempotenciaRepository;
        _transferenciaRepository = transferenciaRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<ApplicationResult> Handle(TransferirCommand request, CancellationToken cancellationToken)
    {
        var validationResult = Validate(request);
        if (validationResult is not null)
        {
            return validationResult;
        }
        var existingRequest = await _idempotenciaRepository.ObterPorRequisicaoAsync(request.IdRequisicao, cancellationToken);
        if (existingRequest is not null)
        {
            return existingRequest.Status
                ? ApplicationResult.FromStoredResult(existingRequest.StatusCode, existingRequest.Resultado)
                : ApplicationResult.From(409, "Requisição em processamento", ResponseTypes.TypeAlreadyExists, null);
        }
        var inserted = await _idempotenciaRepository.InserirEmAndamentoAsync(request.IdRequisicao, cancellationToken);
        if (!inserted)
        {
            var concurrentRequest = await _idempotenciaRepository.ObterPorRequisicaoAsync(request.IdRequisicao, cancellationToken);
            if (concurrentRequest is not null)
            {
                return concurrentRequest.Status
                    ? ApplicationResult.FromStoredResult(concurrentRequest.StatusCode, concurrentRequest.Resultado)
                    : ApplicationResult.From(409, "Requisição em processamento", ResponseTypes.TypeAlreadyExists, null);
            }
            return ApplicationResult.From(409, "Requisição em processamento", ResponseTypes.TypeAlreadyExists, null);
        }
        var contaDestino = await _contaCorrenteRepository.ObterPorIdAsync(request.ContaDestino, cancellationToken);
        if (contaDestino is null)
        {
            return await FinalizeIdempotencyAsync(
                request.IdRequisicao,
                ApplicationResult.From(404, "Conta de destino não localizada", ResponseTypes.TypeInvalidAccount, null),
                cancellationToken);
        }
        if (!contaDestino.Ativo)
        {
            return await FinalizeIdempotencyAsync(
                request.IdRequisicao,
                ApplicationResult.From(409, "Conta de destino está inativa", ResponseTypes.TypeInactiveAccount, null),
                cancellationToken);
        }
        var debitoResult = await _contaCorrenteClient.MovimentarAsync(request.IdContaOrigem, request.Valor, "D", cancellationToken);
        if (debitoResult.StatusCode != 204)
        {
            return await FinalizeIdempotencyAsync(request.IdRequisicao, ToApplicationResult(debitoResult), cancellationToken);
        }
        var creditoResult = await _contaCorrenteClient.MovimentarAsync(request.ContaDestino, request.Valor, "C", cancellationToken);
        if (creditoResult.StatusCode != 204)
        {
            var estornoResult = await _contaCorrenteClient.MovimentarAsync(request.IdContaOrigem, request.Valor, "C", cancellationToken);
            if (estornoResult.StatusCode != 204)
            {
                var creditoApplicationResult = ToApplicationResult(creditoResult);
                var estornoApplicationResult = ToApplicationResult(estornoResult);
                var criticalResult = ApplicationResult.From(
                    409,
                    "Falha crítica ao estornar transferência",
                    ResponseTypes.TypeCriticalFailure,
                    new
                    {
                        falha_transferencia = creditoApplicationResult.Envelope,
                        falha_estorno = estornoApplicationResult.Envelope
                    });
                Trace.TraceError(
                    "Falha crítica no estorno da transferência. Origem: {0}, Destino: {1}, Requisicao: {2}",
                    request.IdContaOrigem,
                    request.ContaDestino,
                    request.IdRequisicao);
                return await FinalizeIdempotencyAsync(request.IdRequisicao, criticalResult, cancellationToken);
            }
            return await FinalizeIdempotencyAsync(request.IdRequisicao, ToApplicationResult(creditoResult), cancellationToken);
        }
        await using var connection = await _unitOfWork.CreateOpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        await _transferenciaRepository.InserirAsync(
            new TransferenciaEntity
            {
                IdTransferencia = Guid.NewGuid(),
                IdContaOrigem = request.IdContaOrigem,
                IdContaDestino = request.ContaDestino,
                DataMvto = DateTimeOffset.UtcNow,
                Valor = request.Valor
            },
            cancellationToken,
            connection,
            transaction);
        await _idempotenciaRepository.AtualizarResultadoAsync(
            request.IdRequisicao,
            true,
            "204",
            null,
            cancellationToken,
            connection,
            transaction);
        await transaction.CommitAsync(cancellationToken);
        return ApplicationResult.NoContent();
    }

    private static ApplicationResult? Validate(TransferirCommand request)
    {
        if (!Validators.IsGuidValido(request.IdContaOrigem))
        {
            return ApplicationResult.From(401, "Usuário não autorizado", ResponseTypes.TypeUserUnauthorized, null);
        }
        if (!Validators.IsGuidValido(request.IdRequisicao))
        {
            return ApplicationResult.From(400, "Id da requisição inválido", ResponseTypes.TypeInvalidValue, null);
        }
        if (!Validators.IsGuidValido(request.ContaDestino))
        {
            return ApplicationResult.From(400, "Conta de destino inválida", ResponseTypes.TypeInvalidValue, null);
        }
        if (!Validators.IsValorMonetarioValido(request.Valor))
        {
            return ApplicationResult.From(400, "Valor inválido", ResponseTypes.TypeInvalidValue, null);
        }
        if (request.IdContaOrigem == request.ContaDestino)
        {
            return ApplicationResult.From(400, "Transferência para a própria conta não é permitida", ResponseTypes.TypeOperationNotAllowed, null);
        }
        return null;
    }

    private async Task<ApplicationResult> FinalizeIdempotencyAsync(
        Guid idRequisicao,
        ApplicationResult result,
        CancellationToken cancellationToken)
    {
        var json = result.Envelope is null ? null : JsonSerializer.Serialize(result.Envelope);
        await _idempotenciaRepository.AtualizarResultadoAsync(
            idRequisicao,
            true,
            result.StatusCode.ToString("000"),
            json,
            cancellationToken);
        return result;
    }

    private static ApplicationResult ToApplicationResult(ContaCorrenteOperationResult result)
    {
        if (result.StatusCode == 204)
        {
            return ApplicationResult.NoContent();
        }
        return ApplicationResult.From(
            result.StatusCode,
            result.Message ?? "Falha ao processar integração com ContaCorrente",
            result.Type ?? ResponseTypes.TypeIntegrationError,
            result.Data);
    }

}
