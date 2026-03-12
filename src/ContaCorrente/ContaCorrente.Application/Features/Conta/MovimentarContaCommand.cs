using ContaCorrente.Application.Common;
using MediatR;

namespace ContaCorrente.Application.Features.Conta;

public sealed record MovimentarContaCommand(Guid IdContaJwt, Guid Conta, Guid IdRequisicao, decimal Valor, string? Tipo) : IRequest<ApplicationResult>;
