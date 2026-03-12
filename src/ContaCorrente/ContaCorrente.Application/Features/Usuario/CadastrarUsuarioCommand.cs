using ContaCorrente.Application.Common;
using MediatR;

namespace ContaCorrente.Application.Features.Usuario;

public sealed record CadastrarUsuarioCommand(string? Nome, string? Cpf, string? Senha) : IRequest<ApplicationResult>;
