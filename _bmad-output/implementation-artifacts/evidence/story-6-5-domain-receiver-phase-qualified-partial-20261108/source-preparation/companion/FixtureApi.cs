namespace Hexalith.PrivateDomainReceiverFixture;

/// <summary>Exposes only closed primitive facts and opaque prepared handles to the reflection driver.</summary>
public static class FixtureApi
{
    /// <summary>Gets the fixed case names.</summary>
    public static string[] Names() => CaseCatalog.All.Select(item => item.Name).ToArray();
    /// <summary>Completes all catalog setup before the diagnostic window.</summary>
    public static Task<object> CreateAsync(string caseName, Action<int>? syntheticPhaseProbe)
        => Task.FromResult<object>(new CaseHandle(CaseCatalog.All.Single(item => item.Name == caseName), syntheticPhaseProbe));
    /// <summary>Starts the entire business task synchronously, restoring caller suppression before return.</summary>
    public static Task<object[]> ExecuteAsync(object preparedHandle, bool suppressEntry = false)
    {
        if (!suppressEntry) return ((CaseHandle)preparedHandle).ExecuteAsync(false);
        using (ExecutionContext.SuppressFlow()) return ((CaseHandle)preparedHandle).ExecuteAsync(true);
    }
    /// <summary>Releases fixture-owned resources after the business task has completed.</summary>
    public static Task DisposeAsync(object preparedHandle) => ((CaseHandle)preparedHandle).DisposeAsync();
}
