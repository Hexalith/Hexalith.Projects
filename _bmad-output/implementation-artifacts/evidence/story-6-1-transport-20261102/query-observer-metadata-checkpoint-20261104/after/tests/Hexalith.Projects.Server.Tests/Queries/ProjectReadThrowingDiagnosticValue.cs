#if HEXALITH_SOURCE_FIXTURE
namespace Hexalith.Projects.Server.Tests.Queries;

/// <summary>Proves that unsupported provider values remain visible as explicit failures.</summary>
internal sealed class ProjectReadThrowingDiagnosticValue
{
    /// <inheritdoc/>
    public override string ToString() => throw new InvalidOperationException("diagnostic-render-control");
}
#endif
