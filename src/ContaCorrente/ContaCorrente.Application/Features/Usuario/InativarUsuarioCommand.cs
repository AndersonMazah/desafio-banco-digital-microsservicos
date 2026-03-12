using ContaCorrente.Application.Common;
using MediatR;

namespace ContaCorrente.Application.Features.Usuario;

public sealed record InativarUsuarioCommand(Guid IdContaCorrente, string? Senha) : IRequest<ApplicationResult>;
