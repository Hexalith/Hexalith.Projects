namespace Hexalith.Projects.Integration.Tests;

/// <summary>One physical, freshly read state-store value and its concurrency identity.</summary>
internal sealed record PackageReadStateSnapshot(string Key, byte[] Bytes, string ETag);
