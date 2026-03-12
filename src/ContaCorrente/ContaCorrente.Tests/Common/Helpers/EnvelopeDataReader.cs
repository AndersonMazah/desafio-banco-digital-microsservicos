using System.Text.Json;

namespace ContaCorrente.Tests.Common.Helpers;

public static class EnvelopeDataReader
{
    public static JsonElement ToJsonElement(object? data)
    {
        return JsonSerializer.SerializeToElement(data);
    }

}
