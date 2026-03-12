using ContaCorrente.Application.Common;
using MediatR;

namespace ContaCorrente.Application.Features.Auth;

public sealed record LoginCommand(string? Conta, string? Cpf, string? Senha) : IRequest<ApplicationResult>;
