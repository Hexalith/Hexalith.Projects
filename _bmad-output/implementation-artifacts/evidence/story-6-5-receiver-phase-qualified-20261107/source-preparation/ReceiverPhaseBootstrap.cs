namespace Hexalith.PrivateCaptureCounter;

using System.Reflection;
using System.Runtime.CompilerServices;

/// <summary>Selects only three fixed receiver entry identities before the closed recorder can initialize.</summary>
internal static class ReceiverPhaseBootstrap
{
    /// <summary>Observes fresh entry metadata and restores prior execution-context flow synchronously before original middleware work.</summary>
    internal static CaptureCounters? Begin()
    {
        try
        {
            Role role = Assembly.GetEntryAssembly()?.GetName().Name switch
            {
                "Hexalith.Projects.Server" => Role.Projects,
                "Hexalith.Tenants" => Role.Tenants,
                "Hexalith.Folders.Server" => Role.Folders,
                _ => Role.Unsupported,
            };
            if (role == Role.Unsupported) return null;
            if (ExecutionContext.IsFlowSuppressed()) return BeginWithoutFlow(role);
            using (ExecutionContext.SuppressFlow()) return BeginWithoutFlow(role);
        }
        catch { return null; }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static CaptureCounters? BeginWithoutFlow(Role role) => CaptureCounters.Begin(role);
}
