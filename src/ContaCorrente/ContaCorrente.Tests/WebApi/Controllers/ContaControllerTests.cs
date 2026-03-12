using System.Security.Claims;
using ContaCorrente.Application.Features.Conta;
using ContaCorrente.WebApi.Contracts;
using ContaCorrente.WebApi.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ContaCorrente.Tests.WebApi.Controllers;

public sealed class ContaControllerTests
{
    [Fact]
    public async Task Movimentar_DeveRetornarUnauthorized_QuandoTokenForInvalido()
    {
        var mediator = new Mock<MediatR.IMediator>(MockBehavior.Strict);
        var controller = CreateController(mediator.Object, "token-invalido");
        var action = await controller.Movimentar(new MovimentarContaRequest
        {
            Conta = Guid.NewGuid(),
            IdRequisicao = Guid.NewGuid(),
            Tipo = "D",
            Valor = 10m
        }, CancellationToken.None);
        var result = action.Should().BeOfType<UnauthorizedObjectResult>().Subject;
        result.StatusCode.Should().Be(StatusCodes.Status401Unauthorized);
        mediator.Verify(x => x.Send(It.IsAny<MovimentarContaCommand>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Saldo_DeveRetornarUnauthorized_QuandoTokenForInvalido()
    {
        var mediator = new Mock<MediatR.IMediator>(MockBehavior.Strict);
        var controller = CreateController(mediator.Object, "token-invalido");
        var action = await controller.Saldo(CancellationToken.None);
        var result = action.Should().BeOfType<UnauthorizedObjectResult>().Subject;
        result.StatusCode.Should().Be(StatusCodes.Status401Unauthorized);
        mediator.Verify(x => x.Send(It.IsAny<ConsultarSaldoQuery>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Consultar_DeveDespacharQueryCorreta()
    {
        var mediator = new Mock<MediatR.IMediator>();
        mediator.Setup(x => x.Send(
                It.Is<ConsultarContaPorCpfQuery>(q => q.Cpf == "12345678909"),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(ContaCorrente.Application.Common.ApplicationResult.From(200, "Consulta de Cliente", ContaCorrente.Application.Common.ResponseTypes.TypeSuccess, new { }));
        var controller = CreateController(mediator.Object, Guid.NewGuid().ToString());
        var action = await controller.Consultar(new ConsultarContaRequest { Cpf = "12345678909" }, CancellationToken.None);
        action.Should().BeOfType<ObjectResult>();
        mediator.VerifyAll();
    }

    private static ContaController CreateController(MediatR.IMediator mediator, string nameIdentifier)
    {
        return new ContaController(mediator)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, nameIdentifier)], "Test"))
                }
            }
        };
    }

}
