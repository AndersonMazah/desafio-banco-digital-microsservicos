using Transferencia.Application.Features.Transferencia;

namespace Transferencia.Tests.Common.Builders;

internal sealed class TransferirCommandBuilder
{
    private Guid _idContaOrigem = Guid.NewGuid();
    private Guid _idRequisicao = Guid.NewGuid();
    private Guid _contaDestino = Guid.NewGuid();
    private decimal _valor = 123.45m;

    public TransferirCommandBuilder ComContaOrigem(Guid value)
    {
        _idContaOrigem = value;
        return this;
    }

    public TransferirCommandBuilder ComIdRequisicao(Guid value)
    {
        _idRequisicao = value;
        return this;
    }

    public TransferirCommandBuilder ComContaDestino(Guid value)
    {
        _contaDestino = value;
        return this;
    }

    public TransferirCommandBuilder ComValor(decimal value)
    {
        _valor = value;
        return this;
    }

    public TransferirCommand Build()
    {
        return new TransferirCommand(_idContaOrigem, _idRequisicao, _contaDestino, _valor);
    }

}
