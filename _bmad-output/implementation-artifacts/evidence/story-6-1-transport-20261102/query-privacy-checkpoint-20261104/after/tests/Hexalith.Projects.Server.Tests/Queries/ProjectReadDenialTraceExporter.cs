#if HEXALITH_SOURCE_FIXTURE
namespace Hexalith.Projects.Server.Tests.Queries;

using System.Diagnostics;
using OpenTelemetry;

/// <summary>Forwards every span from the real SDK tracer provider's export boundary.</summary>
/// <param name="observer">The complete observation sink.</param>
internal sealed class ProjectReadDenialTraceExporter(ProjectReadDenialObserver observer) : BaseExporter<Activity>
{
    /// <inheritdoc/>
    public override ExportResult Export(in Batch<Activity> batch)
    {
        foreach (Activity activity in batch) observer.RecordExportedActivity(activity);
        return ExportResult.Success;
    }
}
#endif
