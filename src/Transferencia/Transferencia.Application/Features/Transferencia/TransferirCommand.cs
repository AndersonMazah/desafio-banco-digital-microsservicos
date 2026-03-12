using MediatR;
using Transferencia.Application.Common;

namespace Transferencia.Application.Features.Transferencia;

public sealed record TransferirCommand(
    Guid IdContaOrigem,
    Guid IdRequisicao,
    Guid ContaDestino,
    decimal Valor) : IRequest<ApplicationResult>;
