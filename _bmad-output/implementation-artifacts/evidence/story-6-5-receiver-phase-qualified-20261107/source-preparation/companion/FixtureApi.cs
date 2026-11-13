namespace Hexalith.PrivateReceiverFixture;

/// <summary>Exposes only opaque setup handles and closed primitive results to the thin entry.</summary>
public static class FixtureApi
{
    /// <summary>Gets the fixed sixteen scenario names.</summary>
    public static string[] Names() => CaseCatalog.All.Select(value => value.Name).ToArray();

    /// <summary>Prepares actual middleware and snapshots before the recorder window without touching the helper.</summary>
    public static async Task<object> CreateAsync(string caseName, Action<int>? syntheticFlowProbe)
        => await CaseHandle.CreateAsync(CaseCatalog.Find(caseName), syntheticFlowProbe).ConfigureAwait(false);

    /// <summary>Runs one actual middleware entry and returns only the documented closed slots.</summary>
    public static Task<object[]> ExecuteAsync(object opaqueHandle, bool suppressEntry)
        => ((CaseHandle)opaqueHandle).ExecuteAsync(suppressEntry);

    /// <summary>Disposes only the fixture's owned application resources after all original work completes.</summary>
    public static Task DisposeAsync(object opaqueHandle) => ((CaseHandle)opaqueHandle).DisposeAsync();
}
