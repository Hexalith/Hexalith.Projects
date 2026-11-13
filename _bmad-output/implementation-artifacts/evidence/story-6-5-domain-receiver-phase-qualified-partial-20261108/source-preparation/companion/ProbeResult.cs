using Microsoft.AspNetCore.Http;
namespace Hexalith.PrivateDomainReceiverFixture;

/// <summary>Runs a controlled real IResult callback only when the returned wrapper is actually executed.</summary>
internal sealed class ProbeResult(Func<HttpContext, Task> execute) : IResult
{
    public Task ExecuteAsync(HttpContext httpContext) => execute(httpContext);
}
