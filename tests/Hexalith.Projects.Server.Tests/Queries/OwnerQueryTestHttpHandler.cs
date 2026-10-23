namespace Hexalith.Projects.Server.Tests.Queries;

/// <summary>Executes a controlled response at the real delegated owner's HTTP boundary.</summary>
/// <param name="send">The request assertions and controlled response.</param>
internal sealed class OwnerQueryTestHttpHandler(
    Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> send) : HttpMessageHandler
{
    /// <inheritdoc/>
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        => send(request, cancellationToken);
}
