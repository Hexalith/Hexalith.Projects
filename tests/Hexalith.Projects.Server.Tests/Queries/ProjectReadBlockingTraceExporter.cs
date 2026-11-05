#if HEXALITH_SOURCE_FIXTURE
namespace Hexalith.Projects.Server.Tests.Queries;

using System.Diagnostics;
using OpenTelemetry;

/// <summary>Blocks a real synchronous export until the control explicitly releases it.</summary>
/// <param name="observer">The actual export sink.</param>
internal sealed class ProjectReadBlockingTraceExporter(ProjectReadDenialObserver observer) : BaseExporter<Activity>
{
    private readonly ManualResetEventSlim _release = new();

    /// <summary>Gets proof that the provider has entered its export callback.</summary>
    internal TaskCompletionSource Entered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

    /// <summary>Allows the actual export to finish.</summary>
    internal void Release() => _release.Set();

    /// <inheritdoc/>
    public override ExportResult Export(in Batch<Activity> batch)
    {
        Entered.TrySetResult();
        if (!_release.Wait(TimeSpan.FromSeconds(5))) return ExportResult.Failure;
        foreach (Activity activity in batch) observer.RecordExportedActivity(activity);
        return ExportResult.Success;
    }

    /// <inheritdoc/>
    protected override void Dispose(bool disposing)
    {
        if (disposing) _release.Dispose();
        base.Dispose(disposing);
    }
}
#endif
