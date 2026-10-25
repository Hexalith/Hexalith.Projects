#if HEXALITH_SOURCE_FIXTURE
namespace Hexalith.Projects.Server.Tests.Queries;

using Hexalith.EventStore.Contracts.Queries;

/// <summary>An in-memory observation of a real receiver request; assertions never leave the fixture.</summary>
/// <param name="Target">The real receiving workload.</param>
/// <param name="Query">The envelope observed after optional fault injection.</param>
/// <param name="Assertion">The original in-memory signed assertion.</param>
/// <param name="Status">The real receiver HTTP status.</param>
internal sealed record ProjectAuthorityHop(string Target, QueryEnvelope Query, string Assertion, int Status);
#endif
