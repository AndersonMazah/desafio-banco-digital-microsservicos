using ContaCorrente.Application.Common;
using ContaCorrente.Application.Features.Conta;
using ContaCorrente.Domain.Interfaces;
using ContaCorrente.Tests.Common.Builders;
using ContaCorrente.Tests.Common.Helpers;

namespace ContaCorrente.Tests.Application.Handlers;

public sealed class ConsultarSaldoHandlerTests
{
    private readonly Mock<IContaCorrenteRepository> _contaRepository = new();
    private readonly Mock<ISaldoRepository> _saldoRepository = new();

    [Fact]
    public async Task Handle_DeveRetornarErro_QuandoContaNaoExistir()
    {
        var handler = CreateHandler();
        _contaRepository.Setup(x => x.ObterPorIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>(), null, null))
            .ReturnsAsync((ContaCorrente.Domain.Entities.ContaCorrente?)null);
        var result = await handler.Handle(new ConsultarSaldoQuery(Guid.NewGuid()), CancellationToken.None);
        result.StatusCode.Should().Be(404);
        result.Envelope!.Type.Should().Be(ResponseTypes.TypeInvalidAccount);
    }

    [Fact]
    public async Task Handle_DeveRetornarErro_QuandoContaEstiverInativa()
    {
        var conta = new ContaCorrenteBuilder().Ativa(false).Build();
        var handler = CreateHandler();
        _contaRepository.Setup(x => x.ObterPorIdAsync(conta.IdContaCorrente, It.IsAny<CancellationToken>(), null, null)).ReturnsAsync(conta);
        var result = await handler.Handle(new ConsultarSaldoQuery(conta.IdContaCorrente), CancellationToken.None);
        result.StatusCode.Should().Be(409);
        result.Envelope!.Type.Should().Be(ResponseTypes.TypeInactiveAccount);
    }

    [Fact]
    public async Task Handle_DeveRetornarSaldoZero_QuandoContaNaoPossuirMovimentacoes()
    {
        var conta = new ContaCorrenteBuilder().Build();
        var handler = CreateHandler();
        _contaRepository.Setup(x => x.ObterPorIdAsync(conta.IdContaCorrente, It.IsAny<CancellationToken>(), null, null)).ReturnsAsync(conta);
        _saldoRepository.Setup(x => x.ObterSaldoAsync(conta.IdContaCorrente, It.IsAny<CancellationToken>())).ReturnsAsync(0m);
        var result = await handler.Handle(new ConsultarSaldoQuery(conta.IdContaCorrente), CancellationToken.None);
        result.StatusCode.Should().Be(200);
        var data = EnvelopeDataReader.ToJsonElement(result.Envelope!.Data);
        data.GetProperty("conta").GetInt64().Should().Be(conta.Numero);
        data.GetProperty("saldo").GetDecimal().Should().Be(0m);
    }

    private ConsultarSaldoHandler CreateHandler()
    {
        return new ConsultarSaldoHandler(_contaRepository.Object, _saldoRepository.Object);
    }

}
