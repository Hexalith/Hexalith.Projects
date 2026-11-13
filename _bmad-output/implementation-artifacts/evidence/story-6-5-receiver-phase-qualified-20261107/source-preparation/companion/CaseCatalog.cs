namespace Hexalith.PrivateReceiverFixture;

/// <summary>The reviewed sixteen actual-middleware scenarios; no request values enter this catalog.</summary>
internal static class CaseCatalog
{
    /// <summary>Gets all closed scenarios.</summary>
    internal static IReadOnlyList<CaseSpec> All { get; } =
    [
        new("bypass", false, "bypass", 204, null, 0, 1, 0, 0, 1),
        new("invalid-channel", false, "channel", 401, null, 0, 0, 0, 0, 1),
        new("native-config-discovery", false, "discovery", 204, null, 0, 1, 0, 0, 3),
        new("native-allows-refusal", false, "allows", 403, null, 0, 0, 0, 0, 3),
        new("receiver-not-ready", false, "ready", 403, null, 0, 0, 0, 0, 3),
        new("native-success", false, "none", 204, null, 1, 1, 1, 0, 191),
        new("native-acquire-refusal", false, "acquire", 503, null, 1, 0, 0, 1, 143),
        new("native-snapshot-refusal", false, "snapshot", 503, null, 1, 0, 1, 0, 175),
        new("native-next-fault", false, "next", 200, typeof(InvalidOperationException), 1, 1, 0, 1, 191),
        new("native-next-cancellation", false, "cancel", 200, typeof(OperationCanceledException), 1, 1, 0, 1, 191),
        new("technical-auth-refusal", true, "auth", 401, null, 0, 0, 0, 0, 71),
        new("technical-success", true, "none", 204, null, 1, 1, 1, 0, 127),
        new("technical-grant-denial", true, "grant", 403, null, 1, 0, 1, 0, 111),
        new("technical-double-dispose-retry-success", true, "release-first", 503, null, 1, 0, 2, 1, 111),
        new("technical-double-dispose-second-fault", true, "release-both", 200, typeof(InvalidOperationException), 1, 0, 2, 2, 111),
        new("technical-next-fault-release-precedence", true, "next-release", 200, typeof(IOException), 1, 1, 1, 2, 127),
    ];

    /// <summary>Selects one known case or refuses before any fixture is created.</summary>
    internal static CaseSpec Find(string name) => All.SingleOrDefault(value => value.Name == name)
        ?? throw new FixtureFailure("unknown-closed-case");
}
