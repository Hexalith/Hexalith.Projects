#if SUPPORTED_PROJECT_READ_DISTRIBUTED
namespace Hexalith.Projects.Integration.Tests;

/// <summary>One physical, freshly read state-store value and its concurrency identity.</summary>
internal sealed record SupportedProjectReadStateSnapshot(string Key, byte[] Bytes, string ETag);
#endif
