using System.Security.Claims;
using ContaCorrente.Application.Features.Usuario;
using ContaCorrente.WebApi.Contracts;
using ContaCorrente.WebApi.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ContaCorrente.Tests.WebApi.Controllers;

public sealed class UsuarioControllerTests
{
    [Fact]
    public async Task Inativar_DeveRetornarUnauthorized_QuandoTokenForInvalido()
    {
        var mediator = new Mock<MediatR.IMediator>(MockBehavior.Strict);
        var controller = new UsuarioController(mediator.Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, "invalido")], "Test"))
                }
            }
        };
        var action = await controller.Inativar(new InativarUsuarioRequest { Senha = "123456" }, CancellationToken.None);
        var result = action.Should().BeOfType<UnauthorizedObjectResult>().Subject;
        result.StatusCode.Should().Be(StatusCodes.Status401Unauthorized);
        mediator.Verify(x => x.Send(It.IsAny<InativarUsuarioCommand>(), It.IsAny<CancellationToken>()), Times.Never);
    }

}
