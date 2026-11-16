namespace Hexalith.Projects.PackageFixtures;

/// <summary>Bounds unsuccessful readiness observations and preserves cancellation as a fixture failure.</summary>
internal static class FixtureReadiness
{
    internal static async Task AwaitAsync(Func<CancellationToken, Task<bool>> ready, string purpose,
        TimeSpan limit, CancellationToken cancellationToken)
    {
        using CancellationTokenSource timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(limit);
        try
        {
            while (!await ready(timeout.Token).ConfigureAwait(false))
                await Task.Delay(TimeSpan.FromMilliseconds(250), timeout.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException failure)
        {
            throw new InvalidOperationException("Fixture readiness failed: " + purpose, failure);
        }
    }
}
