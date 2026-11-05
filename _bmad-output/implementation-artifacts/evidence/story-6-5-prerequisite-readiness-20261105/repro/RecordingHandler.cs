using System.Net;

/// <summary>Records the relayed credential without sending any network request.</summary>
public sealed class RecordingHandler : HttpMessageHandler
{
    /// <summary>Gets the most recent synthetic bearer.</summary>
    public string? LastBearer { get; private set; }

    /// <inheritdoc />
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        LastBearer = request.Headers.Authorization?.Parameter;
        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
    }
}
