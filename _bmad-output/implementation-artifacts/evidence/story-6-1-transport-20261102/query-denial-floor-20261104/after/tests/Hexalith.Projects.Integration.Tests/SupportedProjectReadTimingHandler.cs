namespace Hexalith.Projects.Integration.Tests;

using System.Diagnostics;
using System.Net;

/// <summary>Observes real transport header completion without changing HttpClient body buffering or timeouts.</summary>
/// <param name="inner">The ordinary real HTTP transport.</param>
/// <param name="observe">The bounded safe-denial timing observation.</param>
internal sealed class SupportedProjectReadTimingHandler(HttpMessageHandler inner, Func<TimeSpan, Task> observe) : DelegatingHandler(inner)
{
    /// <inheritdoc/>
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        long started = Stopwatch.GetTimestamp();
        HttpResponseMessage response = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
        TimeSpan elapsed = Stopwatch.GetElapsedTime(started);
        if (request.Method == HttpMethod.Post && request.RequestUri?.AbsolutePath == "/api/v1/queries"
            && response.StatusCode == HttpStatusCode.NotFound)
        {
            try { await observe(elapsed).ConfigureAwait(false); }
            catch { response.Dispose(); throw; }
        }
        return response;
    }
}
