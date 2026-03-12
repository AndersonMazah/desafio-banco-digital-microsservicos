using ContaCorrente.Application.Common;
using MediatR;

namespace ContaCorrente.Application.Features.Conta;

public sealed record ConsultarSaldoQuery(Guid IdContaJwt) : IRequest<ApplicationResult>;
