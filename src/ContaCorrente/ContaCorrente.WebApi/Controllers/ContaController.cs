using System.Security.Claims;
using ContaCorrente.Application.Features.Conta;
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
[Route("conta")]
[Authorize]
public sealed class ContaController : ControllerBase
{
    private readonly IMediator _mediator;

    public ContaController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost("movimentar")]
    [SwaggerOperation(Summary = "Movimentaa a Conta", Description = "1) Valida JWT; 2) Valida entrada; 3) Trabalha com idempotência; 4) Se tudo estiver correto, então insere no banco;")]
    [SwaggerRequestExample(typeof(MovimentarContaRequest), typeof(MovimentarContaRequestExample))]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Movimentar([FromBody] MovimentarContaRequest request, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var idContaJwt))
        {
            return Unauthorized(new { message = "Usuário não autorizado", type = "TYPE_USER_UNAUTHORIZED", data = (object?)null });
        }
        var result = await _mediator.Send(new MovimentarContaCommand(idContaJwt, request.Conta, request.IdRequisicao, request.Valor, request.Tipo), cancellationToken);
        return this.ToActionResult(result);
    }

    [HttpGet("saldo")]
    [SwaggerOperation(Summary = "Consulta Saldo", Description = "Consulta saldo da conta.")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Saldo(CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var idContaJwt))
        {
            return Unauthorized(new { message = "Usuário não autorizado", type = "TYPE_USER_UNAUTHORIZED", data = (object?)null });
        }
        var result = await _mediator.Send(new ConsultarSaldoQuery(idContaJwt), cancellationToken);
        return this.ToActionResult(result);
    }

    [HttpPost("consultar")]
    [SwaggerOperation(Summary = "Consulta Conta por CPF", Description = "Retorna número da conta e primeiro nome do titular.")]
    [SwaggerRequestExample(typeof(ConsultarContaRequest), typeof(ConsultarContaRequestExample))]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Consultar([FromBody] ConsultarContaRequest request, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new ConsultarContaPorCpfQuery(request.Cpf), cancellationToken);
        return this.ToActionResult(result);
    }

}
