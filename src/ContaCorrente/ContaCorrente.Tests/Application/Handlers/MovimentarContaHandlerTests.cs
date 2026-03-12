using System.Data.Common;
using System.Text.Json;
using ContaCorrente.Application.Common;
using ContaCorrente.Application.Features.Conta;
using ContaCorrente.Domain.Entities;
using ContaCorrente.Domain.Interfaces;
using ContaCorrente.Tests.Common.Builders;
using ContaCorrente.Tests.Common.Fixtures;
using ContaCorrente.Tests.Common.Helpers;

namespace ContaCorrente.Tests.Application.Handlers;

public sealed class MovimentarContaHandlerTests
{
    private readonly Mock<IContaCorrenteRepository> _contaRepository = new();
    private readonly Mock<IMovimentoRepository> _movimentoRepository = new();
    private readonly Mock<IIdempotenciaRepository> _idempotenciaRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly FakeDbConnection _connection = new();

    [Fact]
    public async Task Handle_DeveRetornarErro_QuandoContaForInvalida()
    {
        var handler = CreateHandler();
        var result = await handler.Handle(new MovimentarContaCommand(Guid.NewGuid(), Guid.Empty, Guid.NewGuid(), 10, "D"), CancellationToken.None);
        result.StatusCode.Should().Be(400);
        result.Envelope!.Message.Should().Be("Conta inválida");
    }

    [Fact]
    public async Task Handle_DeveRetornarErro_QuandoIdRequisicaoForInvalido()
    {
        var handler = CreateHandler();
        var result = await handler.Handle(new MovimentarContaCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.Empty, 10, "D"), CancellationToken.None);
        result.StatusCode.Should().Be(400);
        result.Envelope!.Message.Should().Be("ID Requisição inválido");
    }

    [Fact]
    public async Task Handle_DeveRetornarErro_QuandoValorForInvalido()
    {
        var handler = CreateHandler();
        var result = await handler.Handle(new MovimentarContaCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 0, "D"), CancellationToken.None);
        result.StatusCode.Should().Be(400);
        result.Envelope!.Message.Should().Be("Valor inválido");
    }

    [Fact]
    public async Task Handle_DeveRetornarErro_QuandoTipoForInvalido()
    {
        var handler = CreateHandler();
        var result = await handler.Handle(new MovimentarContaCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 10, "X"), CancellationToken.None);
        result.StatusCode.Should().Be(400);
        result.Envelope!.Message.Should().Be("Tipo inválido");
    }

    [Fact]
    public async Task Handle_DeveRetornarErro_QuandoContaEstiverInativa()
    {
        var conta = new ContaCorrenteBuilder().Ativa(false).Build();
        var handler = CreateHandler();
        SetupTransactionFlow();
        _idempotenciaRepository.Setup(x => x.ObterPorRequisicaoAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((Idempotencia?)null);
        _idempotenciaRepository.Setup(x => x.InserirEmAndamentoAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>(), It.IsAny<DbConnection>(), It.IsAny<DbTransaction>())).ReturnsAsync(true);
        _contaRepository.Setup(x => x.ObterPorIdAsync(conta.IdContaCorrente, It.IsAny<CancellationToken>(), It.IsAny<DbConnection>(), It.IsAny<DbTransaction>())).ReturnsAsync(conta);
        var result = await handler.Handle(new MovimentarContaCommand(Guid.NewGuid(), conta.IdContaCorrente, Guid.NewGuid(), 25, "D"), CancellationToken.None);
        result.StatusCode.Should().Be(409);
        result.Envelope!.Type.Should().Be(ResponseTypes.TypeInactiveAccount);
    }

    [Fact]
    public async Task Handle_DeveRetornarErro_QuandoCreditoForParaMesmaContaDoToken()
    {
        var conta = new ContaCorrenteBuilder().Build();
        var handler = CreateHandler();
        SetupTransactionFlow();
        _idempotenciaRepository.Setup(x => x.ObterPorRequisicaoAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((Idempotencia?)null);
        _idempotenciaRepository.Setup(x => x.InserirEmAndamentoAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>(), It.IsAny<DbConnection>(), It.IsAny<DbTransaction>())).ReturnsAsync(true);
        _contaRepository.Setup(x => x.ObterPorIdAsync(conta.IdContaCorrente, It.IsAny<CancellationToken>(), It.IsAny<DbConnection>(), It.IsAny<DbTransaction>())).ReturnsAsync(conta);
        var result = await handler.Handle(new MovimentarContaCommand(conta.IdContaCorrente, conta.IdContaCorrente, Guid.NewGuid(), 25, "C"), CancellationToken.None);
        result.StatusCode.Should().Be(400);
        result.Envelope!.Type.Should().Be(ResponseTypes.TypeInvalidType);
        _movimentoRepository.Verify(x => x.InserirAsync(It.IsAny<Movimento>(), It.IsAny<CancellationToken>(), It.IsAny<DbConnection>(), It.IsAny<DbTransaction>()), Times.Never);
    }

    [Fact]
    public async Task Handle_DevePersistirMovimentacao_QuandoRequestForValido()
    {
        var conta = new ContaCorrenteBuilder().Build();
        var requestId = Guid.NewGuid();
        var handler = CreateHandler();
        SetupTransactionFlow();
        _idempotenciaRepository.Setup(x => x.ObterPorRequisicaoAsync(requestId, It.IsAny<CancellationToken>())).ReturnsAsync((Idempotencia?)null);
        _idempotenciaRepository.Setup(x => x.InserirEmAndamentoAsync(requestId, It.IsAny<CancellationToken>(), It.IsAny<DbConnection>(), It.IsAny<DbTransaction>())).ReturnsAsync(true);
        _contaRepository.Setup(x => x.ObterPorIdAsync(conta.IdContaCorrente, It.IsAny<CancellationToken>(), It.IsAny<DbConnection>(), It.IsAny<DbTransaction>())).ReturnsAsync(conta);
        var result = await handler.Handle(new MovimentarContaCommand(Guid.NewGuid(), conta.IdContaCorrente, requestId, 150.75m, "D"), CancellationToken.None);
        result.StatusCode.Should().Be(204);
        _movimentoRepository.Verify(x => x.InserirAsync(
            It.Is<Movimento>(m =>
                m.IdContaCorrente == conta.IdContaCorrente &&
                m.Tipo == 'D' &&
                m.Valor == 150.75m),
            It.IsAny<CancellationToken>(),
            It.IsAny<DbConnection>(),
            It.IsAny<DbTransaction>()), Times.Once);
        _idempotenciaRepository.Verify(x => x.AtualizarResultadoAsync(
            requestId,
            "204",
            null,
            It.IsAny<CancellationToken>(),
            It.IsAny<DbConnection>(),
            It.IsAny<DbTransaction>()), Times.Once);
    }

    [Fact]
    public async Task Handle_DeveRetornarConflito_QuandoIdempotenciaEstiverEmAndamento()
    {
        var requestId = Guid.NewGuid();
        var handler = CreateHandler();
        _idempotenciaRepository.Setup(x => x.ObterPorRequisicaoAsync(requestId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Idempotencia { Requisicao = requestId, Status = false, StatusCode = "202" });
        var result = await handler.Handle(new MovimentarContaCommand(Guid.NewGuid(), Guid.NewGuid(), requestId, 10, "D"), CancellationToken.None);
        result.StatusCode.Should().Be(409);
        result.Envelope!.Message.Should().Be("Requisição em processamento");
    }

    [Fact]
    public async Task Handle_DeveRetornarResultadoArmazenado_QuandoIdempotenciaJaTiverConcluido()
    {
        var requestId = Guid.NewGuid();
        var envelope = new ApiEnvelope("Consulta de saldo", ResponseTypes.TypeSuccess, new { conta = 123456, saldo = 0m });
        var stored = JsonSerializer.Serialize(envelope);
        var handler = CreateHandler();
        _idempotenciaRepository.Setup(x => x.ObterPorRequisicaoAsync(requestId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Idempotencia { Requisicao = requestId, Status = true, StatusCode = "200", Resultado = stored });
        var result = await handler.Handle(new MovimentarContaCommand(Guid.NewGuid(), Guid.NewGuid(), requestId, 10, "D"), CancellationToken.None);
        result.StatusCode.Should().Be(200);
        result.Envelope!.Message.Should().Be("Consulta de saldo");
        EnvelopeDataReader.ToJsonElement(result.Envelope.Data).GetProperty("conta").GetInt32().Should().Be(123456);
    }

    private MovimentarContaHandler CreateHandler()
    {
        return new MovimentarContaHandler(
            _contaRepository.Object,
            _movimentoRepository.Object,
            _idempotenciaRepository.Object,
            _unitOfWork.Object);
    }

    private void SetupTransactionFlow()
    {
        _unitOfWork.Setup(x => x.CreateOpenConnectionAsync(It.IsAny<CancellationToken>())).ReturnsAsync(_connection);
    }

}
