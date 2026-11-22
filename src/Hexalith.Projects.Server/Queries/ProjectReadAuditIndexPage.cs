namespace Hexalith.Projects.Server.Queries;

/// <summary>One append-only page of opaque references to immutable security facts.</summary>
/// <param name="Surrogates">At most 128 request surrogates in initial commit order.</param>
internal sealed record ProjectReadAuditIndexPage(string[] Surrogates);
