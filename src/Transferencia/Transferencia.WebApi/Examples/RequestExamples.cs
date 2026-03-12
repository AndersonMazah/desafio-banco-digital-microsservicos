using Transferencia.WebApi.Contracts;
using Swashbuckle.AspNetCore.Filters;

namespace Transferencia.WebApi.Examples;

public sealed class TransferirRequestExample : IExamplesProvider<TransferirRequest>
{
    public TransferirRequest GetExamples()
    {
        return new()
        {
            IdRequisicao = Guid.Parse("7834f293-dfd5-4d27-b3dc-11c4c6f4ac30"),
            ContaDestino = Guid.Parse("61e37965-cc29-4b08-a3df-71bda67565f3"),
            Valor = 123.45m
        };
    }
}
