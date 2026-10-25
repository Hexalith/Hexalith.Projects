#if HEXALITH_SOURCE_FIXTURE
namespace Hexalith.Projects.Server.Tests.Queries;

/// <summary>Persisted bytes and ETag from a fresh state-store HTTP read.</summary>
/// <param name="Key">The exact logical state key.</param>
/// <param name="Bytes">The persisted physical response bytes.</param>
/// <param name="ETag">The persisted concurrency token.</param>
internal sealed record ProjectAuthoritySnapshot(string Key, byte[] Bytes, string ETag);
#endif
