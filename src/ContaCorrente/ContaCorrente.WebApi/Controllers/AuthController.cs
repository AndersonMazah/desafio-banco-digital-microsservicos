using ContaCorrente.Application.Features.Auth;
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
[Route("auth")]
public sealed class AuthController : ControllerBase
{
    private readonly IMediator _mediator;

    public AuthController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    [SwaggerOperation(Summary = "Login", Description = "Valida conta/CPF e senha e retorna token JWT contendo idcontacorrente.")]
    [SwaggerRequestExample(typeof(LoginRequest), typeof(LoginRequestExample))]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new LoginCommand(request.Conta, request.Cpf, request.Senha), cancellationToken);
        return this.ToActionResult(result);
    }

}
