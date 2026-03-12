using System.Data.Common;
using System.Text.Json;
using FluentAssertions;
using Moq;
using Transferencia.Application.Common;
using Transferencia.Application.Features.Transferencia;
using Transferencia.Domain.Entities;
using Transferencia.Domain.Interfaces;
using Transferencia.Domain.Models;
using Transferencia.Tests.Common.Builders;
using Transferencia.Tests.Common.Fakes;
using Xunit;
using TransferenciaEntity = Transferencia.Domain.Entities.Transferencia;

namespace Transferencia.Tests.Application.Handlers;

public sealed class TransferirHandlerTests
{
    [Fact]
    public async Task DeveRetornarErroQuandoContaDestinoForIgualContaOrigem()
    {
        var idConta = Guid.NewGuid();
        var command = new TransferirCommandBuilder()
            .ComContaOrigem(idConta)
            .ComContaDestino(idConta)
            .Build();
        var sut = CreateSut();
        var result = await sut.Handler.Handle(command, CancellationToken.None);
        result.StatusCode.Should().Be(400);
        result.Envelope.Should().BeEquivalentTo(new ApiEnvelope(
            "Transferência para a própria conta não é permitida",
            ResponseTypes.TypeOperationNotAllowed,
            null));
        sut.IdempotenciaRepository.Verify(
            x => x.ObterPorRequisicaoAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task DeveRetornarErroQuandoValorForInvalido(decimal valor)
    {
        var command = new TransferirCommandBuilder()
            .ComValor(valor)
            .Build();
        var sut = CreateSut();
        var result = await sut.Handler.Handle(command, CancellationToken.None);
        result.StatusCode.Should().Be(400);
        result.Envelope.Should().BeEquivalentTo(new ApiEnvelope(
            "Valor inválido",
            ResponseTypes.TypeInvalidValue,
            null));
    }

    [Fact]
    public async Task DeveRetornar409QuandoRequisicaoJaEstiverEmProcessamento()
    {
        var command = new TransferirCommandBuilder().Build();
        var sut = CreateSut();
        sut.IdempotenciaRepository
            .Setup(x => x.ObterPorRequisicaoAsync(command.IdRequisicao, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Idempotencia
            {
                Requisicao = command.IdRequisicao,
                Status = false,
                StatusCode = "202"
            });
        var result = await sut.Handler.Handle(command, CancellationToken.None);
        result.StatusCode.Should().Be(409);
        result.Envelope.Should().BeEquivalentTo(new ApiEnvelope(
            "Requisição em processamento",
            ResponseTypes.TypeAlreadyExists,
            null));
    }

    [Fact]
    public async Task DeveRetornarResultadoSalvoQuandoIdempotenciaJaEstiverConcluida()
    {
        var command = new TransferirCommandBuilder().Build();
        var storedEnvelope = new ApiEnvelope("Resultado anterior", ResponseTypes.TypeSuccess, new { protocolo = "abc" });
        var sut = CreateSut();
        sut.IdempotenciaRepository
            .Setup(x => x.ObterPorRequisicaoAsync(command.IdRequisicao, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Idempotencia
            {
                Requisicao = command.IdRequisicao,
                Status = true,
                StatusCode = "200",
                Resultado = JsonSerializer.Serialize(storedEnvelope)
            });
        var result = await sut.Handler.Handle(command, CancellationToken.None);
        result.StatusCode.Should().Be(200);
        result.Envelope.Should().NotBeNull();
        result.Envelope!.Message.Should().Be(storedEnvelope.Message);
        result.Envelope.Type.Should().Be(storedEnvelope.Type);
        var data = result.Envelope.Data.Should().BeOfType<JsonElement>().Subject;
        data.GetProperty("protocolo").GetString().Should().Be("abc");
        sut.IdempotenciaRepository.Verify(
            x => x.InserirEmAndamentoAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task DeveRetornarErroQuandoContaDestinoNaoForLocalizada()
    {
        var command = new TransferirCommandBuilder().Build();
        var sut = CreateSut();
        ConfigureIdempotenciaNova(sut, command);
        sut.ContaCorrenteRepository
            .Setup(x => x.ObterPorIdAsync(command.ContaDestino, It.IsAny<CancellationToken>(), null, null))
            .ReturnsAsync((ContaCorrente?)null);
        var result = await sut.Handler.Handle(command, CancellationToken.None);
        result.StatusCode.Should().Be(404);
        result.Envelope.Should().BeEquivalentTo(new ApiEnvelope(
            "Conta de destino não localizada",
            ResponseTypes.TypeInvalidAccount,
            null));
        sut.TransferenciaRepository.Verify(
            x => x.InserirAsync(It.IsAny<TransferenciaEntity>(), It.IsAny<CancellationToken>(), It.IsAny<DbConnection>(), It.IsAny<DbTransaction>()),
            Times.Never);
        sut.IdempotenciaRepository.Verify(
            x => x.AtualizarResultadoAsync(command.IdRequisicao, true, "404", It.IsAny<string>(), It.IsAny<CancellationToken>(), null, null),
            Times.Once);
    }

    [Fact]
    public async Task DeveRetornarErroQuandoContaDestinoEstiverInativa()
    {
        var command = new TransferirCommandBuilder().Build();
        var sut = CreateSut();
        ConfigureIdempotenciaNova(sut, command);
        sut.ContaCorrenteRepository
            .Setup(x => x.ObterPorIdAsync(command.ContaDestino, It.IsAny<CancellationToken>(), null, null))
            .ReturnsAsync(new ContaCorrente { IdContaCorrente = command.ContaDestino, Ativo = false });
        var result = await sut.Handler.Handle(command, CancellationToken.None);
        result.StatusCode.Should().Be(409);
        result.Envelope.Should().BeEquivalentTo(new ApiEnvelope(
            "Conta de destino está inativa",
            ResponseTypes.TypeInactiveAccount,
            null));
    }

    [Fact]
    public async Task DevePropagarErroQuandoContaOrigemNaoForLocalizadaNoDebito()
    {
        var command = new TransferirCommandBuilder().Build();
        var sut = CreateSut();
        ConfigureContaDestinoAtiva(sut, command);
        sut.ContaCorrenteClient
            .Setup(x => x.MovimentarAsync(command.IdContaOrigem, command.Valor, "D", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ContaCorrenteOperationResult
            {
                StatusCode = 404,
                Message = "Conta de origem não localizada",
                Type = ResponseTypes.TypeInvalidAccount
            });
        var result = await sut.Handler.Handle(command, CancellationToken.None);
        result.StatusCode.Should().Be(404);
        result.Envelope.Should().BeEquivalentTo(new ApiEnvelope(
            "Conta de origem não localizada",
            ResponseTypes.TypeInvalidAccount,
            null));
        sut.TransferenciaRepository.Verify(
            x => x.InserirAsync(It.IsAny<TransferenciaEntity>(), It.IsAny<CancellationToken>(), It.IsAny<DbConnection>(), It.IsAny<DbTransaction>()),
            Times.Never);
    }

    [Fact]
    public async Task DevePropagarErroQuandoContaOrigemEstiverInativaNoDebito()
    {
        var command = new TransferirCommandBuilder().Build();
        var sut = CreateSut();
        ConfigureContaDestinoAtiva(sut, command);
        sut.ContaCorrenteClient
            .Setup(x => x.MovimentarAsync(command.IdContaOrigem, command.Valor, "D", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ContaCorrenteOperationResult
            {
                StatusCode = 409,
                Message = "Conta de origem está inativa",
                Type = ResponseTypes.TypeInactiveAccount
            });
        var result = await sut.Handler.Handle(command, CancellationToken.None);
        result.StatusCode.Should().Be(409);
        result.Envelope.Should().BeEquivalentTo(new ApiEnvelope(
            "Conta de origem está inativa",
            ResponseTypes.TypeInactiveAccount,
            null));
    }

    [Fact]
    public async Task DevePersistirTransferenciaQuandoDebitoECreditoForemBemSucedidos()
    {
        var command = new TransferirCommandBuilder().Build();
        var fakeConnection = new FakeDbConnection();
        var sut = CreateSut(fakeConnection);
        ConfigureContaDestinoAtiva(sut, command);
        sut.ContaCorrenteClient
            .Setup(x => x.MovimentarAsync(It.IsAny<Guid>(), command.Valor, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ContaCorrenteOperationResult { StatusCode = 204 });
        var result = await sut.Handler.Handle(command, CancellationToken.None);
        result.StatusCode.Should().Be(204);
        sut.IdempotenciaRepository.Verify(
            x => x.InserirEmAndamentoAsync(command.IdRequisicao, It.IsAny<CancellationToken>()),
            Times.Once);
        sut.TransferenciaRepository.Verify(
            x => x.InserirAsync(
                It.Is<TransferenciaEntity>(t =>
                    t.IdContaOrigem == command.IdContaOrigem &&
                    t.IdContaDestino == command.ContaDestino &&
                    t.Valor == command.Valor &&
                    t.IdTransferencia != Guid.Empty),
                It.IsAny<CancellationToken>(),
                fakeConnection,
                It.IsAny<DbTransaction>()),
            Times.Once);
        sut.IdempotenciaRepository.Verify(
            x => x.AtualizarResultadoAsync(command.IdRequisicao, true, "204", null, It.IsAny<CancellationToken>(), fakeConnection, It.IsAny<DbTransaction>()),
            Times.Once);
        fakeConnection.LastTransaction.Should().NotBeNull();
        fakeConnection.LastTransaction!.CommitCalled.Should().BeTrue();
    }

    [Fact]
    public async Task NaoDevePersistirTransferenciaQuandoDebitoFalhar()
    {
        var command = new TransferirCommandBuilder().Build();
        var sut = CreateSut();
        ConfigureContaDestinoAtiva(sut, command);
        sut.ContaCorrenteClient
            .Setup(x => x.MovimentarAsync(command.IdContaOrigem, command.Valor, "D", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ContaCorrenteOperationResult
            {
                StatusCode = 409,
                Message = "Saldo insuficiente",
                Type = "TYPE_INSUFFICIENT_FUNDS"
            });
        var result = await sut.Handler.Handle(command, CancellationToken.None);
        result.StatusCode.Should().Be(409);
        result.Envelope!.Message.Should().Be("Saldo insuficiente");
        sut.TransferenciaRepository.Verify(
            x => x.InserirAsync(It.IsAny<TransferenciaEntity>(), It.IsAny<CancellationToken>(), It.IsAny<DbConnection>(), It.IsAny<DbTransaction>()),
            Times.Never);
        sut.ContaCorrenteClient.Verify(
            x => x.MovimentarAsync(command.ContaDestino, It.IsAny<decimal>(), "C", It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task DeveTentarEstornoQuandoCreditoFalharDepoisDeDebitoBemSucedido()
    {
        var command = new TransferirCommandBuilder().Build();
        var sut = CreateSut();
        ConfigureContaDestinoAtiva(sut, command);
        sut.ContaCorrenteClient
            .Setup(x => x.MovimentarAsync(command.IdContaOrigem, command.Valor, "D", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ContaCorrenteOperationResult { StatusCode = 204 });
        sut.ContaCorrenteClient
            .Setup(x => x.MovimentarAsync(command.ContaDestino, command.Valor, "C", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ContaCorrenteOperationResult
            {
                StatusCode = 409,
                Message = "Falha no crédito",
                Type = ResponseTypes.TypeIntegrationError
            });
        sut.ContaCorrenteClient
            .Setup(x => x.MovimentarAsync(command.IdContaOrigem, command.Valor, "C", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ContaCorrenteOperationResult { StatusCode = 204 });
        var result = await sut.Handler.Handle(command, CancellationToken.None);
        result.StatusCode.Should().Be(409);
        result.Envelope!.Message.Should().Be("Falha no crédito");
        sut.ContaCorrenteClient.Verify(
            x => x.MovimentarAsync(command.IdContaOrigem, command.Valor, "C", It.IsAny<CancellationToken>()),
            Times.Once);
        sut.TransferenciaRepository.Verify(
            x => x.InserirAsync(It.IsAny<TransferenciaEntity>(), It.IsAny<CancellationToken>(), It.IsAny<DbConnection>(), It.IsAny<DbTransaction>()),
            Times.Never);
    }

    [Fact]
    public async Task NaoDevePersistirTransferenciaQuandoCreditoFalharEEstornoForExecutadoComSucesso()
    {
        var command = new TransferirCommandBuilder().Build();
        var sut = CreateSut();
        ConfigureContaDestinoAtiva(sut, command);
        sut.ContaCorrenteClient
            .Setup(x => x.MovimentarAsync(command.IdContaOrigem, command.Valor, "D", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ContaCorrenteOperationResult { StatusCode = 204 });
        sut.ContaCorrenteClient
            .Setup(x => x.MovimentarAsync(command.ContaDestino, command.Valor, "C", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ContaCorrenteOperationResult
            {
                StatusCode = 409,
                Message = "Falha no crédito",
                Type = ResponseTypes.TypeIntegrationError
            });
        sut.ContaCorrenteClient
            .Setup(x => x.MovimentarAsync(command.IdContaOrigem, command.Valor, "C", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ContaCorrenteOperationResult { StatusCode = 204 });
        var result = await sut.Handler.Handle(command, CancellationToken.None);
        result.StatusCode.Should().Be(409);
        sut.TransferenciaRepository.Verify(
            x => x.InserirAsync(It.IsAny<TransferenciaEntity>(), It.IsAny<CancellationToken>(), It.IsAny<DbConnection>(), It.IsAny<DbTransaction>()),
            Times.Never);
    }

    private static void ConfigureIdempotenciaNova(HandlerFixture sut, TransferirCommand command)
    {
        sut.IdempotenciaRepository
            .Setup(x => x.ObterPorRequisicaoAsync(command.IdRequisicao, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Idempotencia?)null);
        sut.IdempotenciaRepository
            .Setup(x => x.InserirEmAndamentoAsync(command.IdRequisicao, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
    }

    private static void ConfigureContaDestinoAtiva(HandlerFixture sut, TransferirCommand command)
    {
        ConfigureIdempotenciaNova(sut, command);
        sut.ContaCorrenteRepository
            .Setup(x => x.ObterPorIdAsync(command.ContaDestino, It.IsAny<CancellationToken>(), null, null))
            .ReturnsAsync(new ContaCorrente { IdContaCorrente = command.ContaDestino, Ativo = true });
    }

    private static HandlerFixture CreateSut(FakeDbConnection? connection = null)
    {
        var contaCorrenteRepository = new Mock<IContaCorrenteRepository>(MockBehavior.Strict);
        var contaCorrenteClient = new Mock<IContaCorrenteClient>(MockBehavior.Strict);
        var idempotenciaRepository = new Mock<IIdempotenciaRepository>(MockBehavior.Strict);
        var transferenciaRepository = new Mock<ITransferenciaRepository>(MockBehavior.Strict);
        var unitOfWork = new Mock<IUnitOfWork>(MockBehavior.Strict);
        unitOfWork
            .Setup(x => x.CreateOpenConnectionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(connection ?? new FakeDbConnection());
        idempotenciaRepository
            .Setup(x => x.AtualizarResultadoAsync(
                It.IsAny<Guid>(),
                It.IsAny<bool>(),
                It.IsAny<string>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<DbConnection?>(),
                It.IsAny<DbTransaction?>()))
            .Returns(Task.CompletedTask);
        transferenciaRepository
            .Setup(x => x.InserirAsync(
                It.IsAny<TransferenciaEntity>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<DbConnection>(),
                It.IsAny<DbTransaction>()))
            .Returns(Task.CompletedTask);
        return new HandlerFixture(
            new TransferirHandler(
                contaCorrenteRepository.Object,
                contaCorrenteClient.Object,
                idempotenciaRepository.Object,
                transferenciaRepository.Object,
                unitOfWork.Object),
            contaCorrenteRepository,
            contaCorrenteClient,
            idempotenciaRepository,
            transferenciaRepository,
            unitOfWork);
    }

    private sealed record HandlerFixture(
        TransferirHandler Handler,
        Mock<IContaCorrenteRepository> ContaCorrenteRepository,
        Mock<IContaCorrenteClient> ContaCorrenteClient,
        Mock<IIdempotenciaRepository> IdempotenciaRepository,
        Mock<ITransferenciaRepository> TransferenciaRepository,
        Mock<IUnitOfWork> UnitOfWork);

}
