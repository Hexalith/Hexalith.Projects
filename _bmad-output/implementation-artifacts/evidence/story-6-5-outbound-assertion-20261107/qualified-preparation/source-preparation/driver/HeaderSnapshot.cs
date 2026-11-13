namespace Story65.OutboundAssertionProof;

using System.Text.Json;

internal static class HeaderSnapshot
{
    internal static string Of(HttpRequestMessage request)
        => JsonSerializer.Serialize(request.Headers.OrderBy(header => header.Key, StringComparer.Ordinal)
            .Select(header => new { header.Key, Values = header.Value.ToArray() }));
}
