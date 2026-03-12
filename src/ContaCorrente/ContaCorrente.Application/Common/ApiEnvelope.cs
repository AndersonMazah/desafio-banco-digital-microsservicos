namespace ContaCorrente.Application.Common;

public sealed record ApiEnvelope(string Message, string Type, object? Data);
