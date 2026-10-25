#if HEXALITH_SOURCE_FIXTURE
namespace Hexalith.Projects.Server.Tests.Queries;

/// <summary>Sanitized receiver attempt recorded before forwarding, including unsigned and malformed assertions.</summary>
/// <param name="Target">The receiving workload.</param>
/// <param name="Method">The invoked route.</param>
/// <param name="AssertionCount">The actual transmitted assertion header value count.</param>
/// <param name="AssertionState">Missing, multiple, malformed, or JWT-shaped.</param>
internal sealed record ProjectAuthorityInvocationAttempt(string Target, string Method, int AssertionCount, string AssertionState)
{
    /// <summary>Gets the sanitized sequence entry.</summary>
    public string Summary => $"service:{Target}:{Method}:{AssertionState}";
}
#endif
