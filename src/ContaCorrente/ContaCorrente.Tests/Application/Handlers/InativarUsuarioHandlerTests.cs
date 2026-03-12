using ContaCorrente.Application.Common;
using ContaCorrente.Application.Features.Usuario;
using ContaCorrente.Tests.Common.Builders;

namespace ContaCorrente.Tests.Application.Handlers;

public sealed class InativarUsuarioHandlerTests
{
    private readonly Mock<ContaCorrente.Domain.Interfaces.IContaCorrenteRepository> _contaRepository = new();
    private readonly Mock<ContaCorrente.Domain.Interfaces.IPasswordHasher> _passwordHasher = new();

    [Fact]
    public async Task Handle_DeveRetornarErro_QuandoSenhaForInvalida()
    {
        var handler = CreateHandler();
        var result = await handler.Handle(new InativarUsuarioCommand(Guid.NewGuid(), "12"), CancellationToken.None);
        result.StatusCode.Should().Be(400);
        result.Envelope!.Message.Should().Be("SENHA inválida");
    }

    [Fact]
    public async Task Handle_DeveRetornarErro_QuandoContaNaoExistir()
    {
        var handler = CreateHandler();
        _contaRepository.Setup(x => x.ObterPorIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>(), null, null))
            .ReturnsAsync((ContaCorrente.Domain.Entities.ContaCorrente?)null);
        var result = await handler.Handle(new InativarUsuarioCommand(Guid.NewGuid(), "123456"), CancellationToken.None);
        result.StatusCode.Should().Be(404);
        result.Envelope!.Type.Should().Be(ResponseTypes.TypeInvalidAccount);
    }

    [Fact]
    public async Task Handle_DeveRetornarErro_QuandoContaJaEstiverInativa()
    {
        var conta = new ContaCorrenteBuilder().Ativa(false).Build();
        var handler = CreateHandler();
        _contaRepository.Setup(x => x.ObterPorIdAsync(conta.IdContaCorrente, It.IsAny<CancellationToken>(), null, null)).ReturnsAsync(conta);
        var result = await handler.Handle(new InativarUsuarioCommand(conta.IdContaCorrente, "123456"), CancellationToken.None);
        result.StatusCode.Should().Be(409);
        result.Envelope!.Message.Should().Be("A conta já está inativa");
    }

    [Fact]
    public async Task Handle_DeveRetornarErro_QuandoSenhaEstiverIncorreta()
    {
        var conta = new ContaCorrenteBuilder().Build();
        var handler = CreateHandler();
        _contaRepository.Setup(x => x.ObterPorIdAsync(conta.IdContaCorrente, It.IsAny<CancellationToken>(), null, null)).ReturnsAsync(conta);
        _passwordHasher.Setup(x => x.Verify("123456", conta.SenhaHash, conta.Salt)).Returns(false);
        var result = await handler.Handle(new InativarUsuarioCommand(conta.IdContaCorrente, "123456"), CancellationToken.None);
        result.StatusCode.Should().Be(401);
        result.Envelope!.Type.Should().Be(ResponseTypes.TypeUserUnauthorized);
    }

    [Fact]
    public async Task Handle_DeveInativarConta_QuandoSenhaEstiverCorreta()
    {
        var conta = new ContaCorrenteBuilder().Build();
        var handler = CreateHandler();
        _contaRepository.Setup(x => x.ObterPorIdAsync(conta.IdContaCorrente, It.IsAny<CancellationToken>(), null, null)).ReturnsAsync(conta);
        _passwordHasher.Setup(x => x.Verify("123456", conta.SenhaHash, conta.Salt)).Returns(true);
        var result = await handler.Handle(new InativarUsuarioCommand(conta.IdContaCorrente, "123456"), CancellationToken.None);
        result.StatusCode.Should().Be(204);
        result.Envelope.Should().BeNull();
        _contaRepository.Verify(x => x.InativarAsync(conta.IdContaCorrente, It.IsAny<CancellationToken>()), Times.Once);
    }

    private InativarUsuarioHandler CreateHandler()
    {
        return new InativarUsuarioHandler(_contaRepository.Object, _passwordHasher.Object);
    }

}
