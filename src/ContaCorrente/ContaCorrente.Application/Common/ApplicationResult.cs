namespace ContaCorrente.Application.Common;

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

}
