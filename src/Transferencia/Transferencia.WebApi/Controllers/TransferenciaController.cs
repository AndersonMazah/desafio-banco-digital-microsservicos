using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using Swashbuckle.AspNetCore.Filters;
using Transferencia.Application.Features.Transferencia;
using Transferencia.WebApi.Contracts;
using Transferencia.WebApi.Examples;
using Transferencia.WebApi.Extensions;

namespace Transferencia.WebApi.Controllers;

[ApiController]
[Route("transferencia")]
[Authorize]
public sealed class TransferenciaController : ControllerBase
{
    private readonly IMediator _mediator;

    public TransferenciaController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost("transferir")]
    [SwaggerOperation(Summary = "Realiza uma transferência entre contas", Description = "1) Valida JWT; 2) Valida entrada; 3) Trabalha com idempotência; 4) integra com a API ContaCorrente;")]
    [SwaggerRequestExample(typeof(TransferirRequest), typeof(TransferirRequestExample))]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Transferir([FromBody] TransferirRequest request, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var idContaOrigem))
        {
            return Unauthorized(new { message = "Usuário não autorizado", type = "TYPE_USER_UNAUTHORIZED", data = (object?)null });
        }
        var result = await _mediator.Send(
            new TransferirCommand(idContaOrigem, request.IdRequisicao, request.ContaDestino, request.Valor),
            cancellationToken);

        return this.ToActionResult(result);
    }

}
