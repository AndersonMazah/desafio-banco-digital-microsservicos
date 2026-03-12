using System.Text.Json;

namespace Transferencia.Application.Common;

public sealed class ApplicationResult
{
    public int StatusCode { get; }
    public ApiEnvelope? Envelope { get; }

    private ApplicationResult(int statusCode, ApiEnvelope? envelope)
    {
        StatusCode = statusCode;
        Envelope = envelope;
    }

    public static ApplicationResult From(int statusCode, string message, string type, object? data)
    {
        return new(statusCode, new ApiEnvelope(message, type, data));
    }

    public static ApplicationResult NoContent()
    {
        return new(204, null);
    }

    public static ApplicationResult FromStoredResult(string statusCode, string? resultado)
    {
        var parsedStatusCode = int.TryParse(statusCode, out var code) ? code : 200;
        if (parsedStatusCode == 204)
        {
            return NoContent();
        }
        if (!string.IsNullOrWhiteSpace(resultado))
        {
            var envelope = JsonSerializer.Deserialize<ApiEnvelope>(resultado);
            if (envelope is not null)
            {
                return new(parsedStatusCode, envelope);
            }
        }
        return From(parsedStatusCode, "Resultado idempotente", ResponseTypes.TypeSuccess, null);
    }

}
