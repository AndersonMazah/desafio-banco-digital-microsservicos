using ContaCorrente.Application.Common;
using ContaCorrente.Application.Features.Auth;
using ContaCorrente.Tests.Common.Builders;

namespace ContaCorrente.Tests.Application.Handlers;

public sealed class LoginHandlerTests
{
    private readonly Mock<ContaCorrente.Domain.Interfaces.IContaCorrenteRepository> _contaRepository = new();
    private readonly Mock<ContaCorrente.Domain.Interfaces.IPasswordHasher> _passwordHasher = new();
    private readonly Mock<ContaCorrente.Domain.Interfaces.IAuthTokenService> _authTokenService = new();

    [Fact]
    public async Task Handle_DeveRetornarToken_QuandoLoginPorContaForValido()
    {
        var conta = new ContaCorrenteBuilder().Build();
        var handler = CreateHandler();
        _contaRepository.Setup(x => x.ObterPorContaOuCpfAsync(conta.Numero, null, It.IsAny<CancellationToken>())).ReturnsAsync(conta);
        _passwordHasher.Setup(x => x.Verify("123456", conta.SenhaHash, conta.Salt)).Returns(true);
        _authTokenService.Setup(x => x.GenerateToken(conta.IdContaCorrente)).Returns("jwt-token");
        var result = await handler.Handle(new LoginCommand(conta.Numero.ToString(), null, "123456"), CancellationToken.None);
        result.StatusCode.Should().Be(200);
        result.Envelope!.Type.Should().Be(ResponseTypes.TypeUserAuthorized);
        result.Envelope.Data.Should().Be("jwt-token");
    }

    [Fact]
    public async Task Handle_DeveRetornarToken_QuandoLoginPorCpfForValido()
    {
        var conta = new ContaCorrenteBuilder().ComCpf("12345678909").Build();
        var handler = CreateHandler();
        _contaRepository.Setup(x => x.ObterPorContaOuCpfAsync(null, "12345678909", It.IsAny<CancellationToken>())).ReturnsAsync(conta);
        _passwordHasher.Setup(x => x.Verify("123456", conta.SenhaHash, conta.Salt)).Returns(true);
        _authTokenService.Setup(x => x.GenerateToken(conta.IdContaCorrente)).Returns("jwt-cpf");
        var result = await handler.Handle(new LoginCommand(null, "123.456.789-09", "123456"), CancellationToken.None);
        result.StatusCode.Should().Be(200);
        result.Envelope!.Data.Should().Be("jwt-cpf");
    }

    [Fact]
    public async Task Handle_DeveRetornarErro_QuandoSenhaForIncorreta()
    {
        var conta = new ContaCorrenteBuilder().Build();
        var handler = CreateHandler();
        _contaRepository.Setup(x => x.ObterPorContaOuCpfAsync(conta.Numero, null, It.IsAny<CancellationToken>())).ReturnsAsync(conta);
        _passwordHasher.Setup(x => x.Verify("123456", conta.SenhaHash, conta.Salt)).Returns(false);
        var result = await handler.Handle(new LoginCommand(conta.Numero.ToString(), null, "123456"), CancellationToken.None);
        result.StatusCode.Should().Be(401);
        result.Envelope!.Type.Should().Be(ResponseTypes.TypeUserUnauthorized);
    }

    [Fact]
    public async Task Handle_DeveRetornarErro_QuandoContaNaoExistir()
    {
        var handler = CreateHandler();
        _contaRepository.Setup(x => x.ObterPorContaOuCpfAsync(123456, null, It.IsAny<CancellationToken>())).ReturnsAsync((ContaCorrente.Domain.Entities.ContaCorrente?)null);
        var result = await handler.Handle(new LoginCommand("123456", null, "123456"), CancellationToken.None);
        result.StatusCode.Should().Be(404);
        result.Envelope!.Type.Should().Be(ResponseTypes.TypeUserUnauthorized);
    }

    [Fact]
    public async Task Handle_DeveRetornarErro_QuandoContaEstiverInativa()
    {
        var conta = new ContaCorrenteBuilder().Ativa(false).Build();
        var handler = CreateHandler();
        _contaRepository.Setup(x => x.ObterPorContaOuCpfAsync(conta.Numero, null, It.IsAny<CancellationToken>())).ReturnsAsync(conta);
        _passwordHasher.Setup(x => x.Verify("123456", conta.SenhaHash, conta.Salt)).Returns(true);
        var result = await handler.Handle(new LoginCommand(conta.Numero.ToString(), null, "123456"), CancellationToken.None);
        result.StatusCode.Should().Be(401);
        result.Envelope!.Type.Should().Be(ResponseTypes.TypeUserUnauthorized);
    }

    [Fact]
    public async Task Handle_DeveRetornarErro_QuandoContaECpfNaoForemInformados()
    {
        var handler = CreateHandler();
        var result = await handler.Handle(new LoginCommand(null, null, "123456"), CancellationToken.None);
        result.StatusCode.Should().Be(400);
        result.Envelope!.Message.Should().Be("É necessário informar a Conta ou o CPF");
    }

    private LoginHandler CreateHandler()
    {
        return new LoginHandler(_contaRepository.Object, _passwordHasher.Object, _authTokenService.Object);
    }

}
