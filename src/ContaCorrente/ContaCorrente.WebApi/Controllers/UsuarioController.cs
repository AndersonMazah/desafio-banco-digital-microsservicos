using System.Security.Claims;
using ContaCorrente.Application.Features.Usuario;
using ContaCorrente.WebApi.Contracts;
using ContaCorrente.WebApi.Examples;
using ContaCorrente.WebApi.Extensions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using Swashbuckle.AspNetCore.Filters;

namespace ContaCorrente.WebApi.Controllers;

[ApiController]
[Route("usuario")]
public sealed class UsuarioController : ControllerBase
{
    private readonly IMediator _mediator;

    public UsuarioController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost("cadastrar")]
    [AllowAnonymous]
    [SwaggerOperation(Summary = "Cadastra o Usuário", Description = "Valida o Nome, CPF e a senha. Se entrada for válida, então, salva no banco e retona numero da conta.")]
    [SwaggerRequestExample(typeof(CadastrarUsuarioRequest), typeof(CadastrarUsuarioRequestExample))]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Cadastrar([FromBody] CadastrarUsuarioRequest request, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new CadastrarUsuarioCommand(request.Nome, request.Cpf, request.Senha), cancellationToken);
        return this.ToActionResult(result);
    }

    [HttpPatch("inativar")]
    [Authorize]
    [SwaggerOperation(Summary = "Inativa o Usuário", Description = "Exige JWT válido. Valida a senha e inativa a conta.")]
    [SwaggerRequestExample(typeof(InativarUsuarioRequest), typeof(InativarUsuarioRequestExample))]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Inativar([FromBody] InativarUsuarioRequest request, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var idConta))
        {
            return Unauthorized(new { message = "Usuário não autorizado", type = "TYPE_USER_UNAUTHORIZED", data = (object?)null });
        }
        var result = await _mediator.Send(new InativarUsuarioCommand(idConta, request.Senha), cancellationToken);
        return this.ToActionResult(result);
    }
}
