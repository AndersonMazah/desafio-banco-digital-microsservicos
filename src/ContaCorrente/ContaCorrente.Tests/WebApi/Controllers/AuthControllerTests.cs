using ContaCorrente.Application.Common;
using ContaCorrente.Application.Features.Auth;
using ContaCorrente.WebApi.Contracts;
using ContaCorrente.WebApi.Controllers;
using Microsoft.AspNetCore.Mvc;

namespace ContaCorrente.Tests.WebApi.Controllers;

public sealed class AuthControllerTests
{
    [Fact]
    public async Task Login_DeveDespacharCommandCorreto()
    {
        var mediator = new Mock<MediatR.IMediator>();
        mediator.Setup(x => x.Send(
                It.Is<LoginCommand>(c => c.Conta == "123456" && c.Cpf == null && c.Senha == "123456"),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApplicationResult.From(200, "Usuário autenticado", ResponseTypes.TypeUserAuthorized, "token"));
        var controller = new AuthController(mediator.Object);
        var action = await controller.Login(new LoginRequest { Conta = "123456", Senha = "123456" }, CancellationToken.None);
        var result = action.Should().BeOfType<ObjectResult>().Subject;
        result.StatusCode.Should().Be(200);
        mediator.VerifyAll();
    }

}
