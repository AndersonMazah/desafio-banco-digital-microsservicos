using ContaCorrente.Application.Common;
using MediatR;

namespace ContaCorrente.Application.Features.Conta;

public sealed record ConsultarContaPorCpfQuery(string? Cpf) : IRequest<ApplicationResult>;
