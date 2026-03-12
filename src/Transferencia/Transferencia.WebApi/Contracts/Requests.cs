using Swashbuckle.AspNetCore.Annotations;
using System.Text.Json.Serialization;

namespace Transferencia.WebApi.Contracts;

public sealed class TransferirRequest
{
    [SwaggerSchema(Description = "UUID idempotente da requisição", Nullable = false)]
    [JsonPropertyName("id_requisicao")]
    public Guid IdRequisicao { get; set; }

    [SwaggerSchema(Description = "UUID da conta de destino", Nullable = false)]
    [JsonPropertyName("conta_destino")]
    public Guid ContaDestino { get; set; }

    [SwaggerSchema(Description = "Valor da transferência (maior que zero)", Nullable = false)]
    public decimal Valor { get; set; }
}
