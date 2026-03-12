using ContaCorrente.Application.Common;
using ContaCorrente.Application.Features.Usuario;
using ContaCorrente.Domain.Interfaces;
using ContaCorrente.Tests.Common.Helpers;

namespace ContaCorrente.Tests.Application.Handlers;

public sealed class CadastrarUsuarioHandlerTests
{
    private readonly Mock<IContaCorrenteRepository> _contaRepository = new();
    private readonly Mock<IPasswordHasher> _passwordHasher = new();

    [Fact]
    public async Task Handle_DeveRetornarSucesso_QuandoCadastroForValido()
    {
        var handler = CreateHandler();
        _passwordHasher.Setup(x => x.HashPassword("123456")).Returns(([1, 2, 3], [4, 5, 6]));
        _contaRepository.Setup(x => x.CpfExisteAsync("12345678909", It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _contaRepository.Setup(x => x.CriarAsync(It.IsAny<ContaCorrente.Domain.Entities.ContaCorrente>(), It.IsAny<CancellationToken>())).ReturnsAsync(987654);
        var result = await handler.Handle(new CadastrarUsuarioCommand("Anderson Silva", "123.456.789-09", "123456"), CancellationToken.None);
        result.StatusCode.Should().Be(201);
        result.Envelope!.Type.Should().Be(ResponseTypes.TypeSuccess);
        EnvelopeDataReader.ToJsonElement(result.Envelope.Data).GetProperty("conta").GetInt64().Should().Be(987654);
    }

    [Fact]
    public async Task Handle_DeveRetornarErro_QuandoCpfForInvalido()
    {
        var handler = CreateHandler();
        var result = await handler.Handle(new CadastrarUsuarioCommand("Anderson Silva", "11111111111", "123456"), CancellationToken.None);
        result.StatusCode.Should().Be(400);
        result.Envelope!.Message.Should().Be("CPF inválido");
        result.Envelope.Type.Should().Be(ResponseTypes.TypeInvalidDocument);
    }

    [Fact]
    public async Task Handle_DeveRetornarConflito_QuandoCpfJaEstiverCadastrado()
    {
        var handler = CreateHandler();
        _contaRepository.Setup(x => x.CpfExisteAsync("12345678909", It.IsAny<CancellationToken>())).ReturnsAsync(true);
        var result = await handler.Handle(new CadastrarUsuarioCommand("Anderson Silva", "12345678909", "123456"), CancellationToken.None);
        result.StatusCode.Should().Be(409);
        result.Envelope!.Message.Should().Be("CPF já cadastrado");
        result.Envelope.Type.Should().Be(ResponseTypes.TypeAlreadyExists);
        _contaRepository.Verify(x => x.CriarAsync(It.IsAny<ContaCorrente.Domain.Entities.ContaCorrente>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_DeveRetornarErro_QuandoNomeForInvalido()
    {
        var handler = CreateHandler();
        var result = await handler.Handle(new CadastrarUsuarioCommand(" ", "12345678909", "123456"), CancellationToken.None);
        result.StatusCode.Should().Be(400);
        result.Envelope!.Message.Should().Be("Nome inválido");
    }

    [Fact]
    public async Task Handle_DeveRetornarErro_QuandoSenhaForInvalida()
    {
        var handler = CreateHandler();
        var result = await handler.Handle(new CadastrarUsuarioCommand("Anderson Silva", "12345678909", "12"), CancellationToken.None);
        result.StatusCode.Should().Be(400);
        result.Envelope!.Message.Should().Contain("SENHA");
        _passwordHasher.Verify(x => x.HashPassword(It.IsAny<string>()), Times.Never);
    }

    private CadastrarUsuarioHandler CreateHandler()
    {
        return new CadastrarUsuarioHandler(_contaRepository.Object, _passwordHasher.Object);
    }

}
