using ContaCorrente.Application.Common;
using ContaCorrente.Application.Features.Conta;
using ContaCorrente.Domain.Interfaces;
using ContaCorrente.Tests.Common.Builders;
using ContaCorrente.Tests.Common.Helpers;

namespace ContaCorrente.Tests.Application.Handlers;

public sealed class ConsultarContaPorCpfHandlerTests
{
    private readonly Mock<IContaCorrenteRepository> _contaRepository = new();

    [Fact]
    public async Task Handle_DeveRetornarErro_QuandoCpfForInvalido()
    {
        var handler = CreateHandler();
        var result = await handler.Handle(new ConsultarContaPorCpfQuery("123"), CancellationToken.None);
        result.StatusCode.Should().Be(400);
        result.Envelope!.Message.Should().Be("CPF inválido");
    }

    [Fact]
    public async Task Handle_DeveRetornarErro_QuandoContaNaoForEncontradaOuEstiverInativa()
    {
        var handler = CreateHandler();
        _contaRepository.Setup(x => x.ObterPorCpfAsync("12345678909", It.IsAny<CancellationToken>()))
            .ReturnsAsync((ContaCorrente.Domain.Entities.ContaCorrente?)null);
        var result = await handler.Handle(new ConsultarContaPorCpfQuery("12345678909"), CancellationToken.None);
        result.StatusCode.Should().Be(404);
        result.Envelope!.Type.Should().Be(ResponseTypes.TypeInvalidAccount);
    }

    [Fact]
    public async Task Handle_DeveRetornarContaComPrimeiroNomeEUuid_QuandoCpfForValido()
    {
        var conta = new ContaCorrenteBuilder().ComNome("Maria Joaquina Souza").Build();
        var handler = CreateHandler();
        _contaRepository.Setup(x => x.ObterPorCpfAsync("12345678909", It.IsAny<CancellationToken>())).ReturnsAsync(conta);
        var result = await handler.Handle(new ConsultarContaPorCpfQuery("123.456.789-09"), CancellationToken.None);
        result.StatusCode.Should().Be(200);
        var data = EnvelopeDataReader.ToJsonElement(result.Envelope!.Data);
        data.GetProperty("uuid").GetGuid().Should().Be(conta.IdContaCorrente);
        data.GetProperty("conta").GetInt64().Should().Be(conta.Numero);
        data.GetProperty("nome").GetString().Should().Be("Maria");
    }

    private ConsultarContaPorCpfHandler CreateHandler()
    {
        return new ConsultarContaPorCpfHandler(_contaRepository.Object);
    }

}
