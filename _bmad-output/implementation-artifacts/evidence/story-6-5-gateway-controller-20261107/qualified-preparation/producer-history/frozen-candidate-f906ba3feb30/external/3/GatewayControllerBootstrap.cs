namespace Hexalith.PrivateCaptureCounter;

using System.Runtime.CompilerServices;

/// <summary>Starts the closed diagnostic recorder without flowing a caller execution context to its worker.</summary>
internal static class GatewayControllerBootstrap
{
    /// <summary>Preserves the caller's synchronous flow state and returns an optional fixed-role recorder.</summary>
    internal static CaptureCounters? Begin()
    {
        try
        {
            if (ExecutionContext.IsFlowSuppressed()) return BeginWithoutFlow();
            using (ExecutionContext.SuppressFlow()) return BeginWithoutFlow();
        }
        catch { return null; }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static CaptureCounters? BeginWithoutFlow() => CaptureCounters.Begin(Role.Gateway);
}
