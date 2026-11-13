namespace Hexalith.PrivateCaptureCounter;

using System.Reflection;
using System.Runtime.CompilerServices;

/// <summary>Selects only the pinned EventStore entry executable before starting the closed diagnostic recorder.</summary>
internal static class InvocationTransportBootstrap
{
    /// <summary>Uses process entry metadata only for diagnostic eligibility and restores prior flow synchronously.</summary>
    internal static CaptureCounters? Begin()
    {
        try
        {
            if (!string.Equals(Assembly.GetEntryAssembly()?.GetName().Name, "Hexalith.EventStore", StringComparison.Ordinal)) return null;
            if (ExecutionContext.IsFlowSuppressed()) return BeginWithoutFlow();
            using (ExecutionContext.SuppressFlow()) return BeginWithoutFlow();
        }
        catch { return null; }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static CaptureCounters? BeginWithoutFlow() => CaptureCounters.Begin(Role.Gateway);
}
